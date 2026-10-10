using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class Bids : ModulePage
    {
        protected Literal TenderNumber, TenderTitle, Message;
        protected HyperLink BackLink;
        protected GridView TechnicalGrid, FinancialGrid;
        protected Panel EvaluatePanel, SelectPanel;
        protected DropDownList TechnicalBidSelect, TechnicalStatus, FinancialBidSelect;
        protected TextBox TechnicalRemarks, SelectionRemarks;
        protected Button EvaluateButton, SelectContractorButton;
        private int TenderId { get { return QueryId("tenderId"); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (TenderId <= 0) { Response.Redirect("~/Pages/Tenders.aspx", false); Context.ApplicationInstance.CompleteRequest(); return; }
            if (!IsPostBack) LoadPage();
        }

        private void LoadPage()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    string currentStatus;
                    using (SqlCommand command = new SqlCommand("SELECT TenderNumber,Title,CurrentStatus FROM dbo.Tender WHERE TenderId=@TenderId AND IsDeleted=0;", connection))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read()) { Response.Redirect("~/Pages/Tenders.aspx", false); Context.ApplicationInstance.CompleteRequest(); return; }
                            TenderNumber.Text = ModuleDb.Html(reader["TenderNumber"]);
                            TenderTitle.Text = ModuleDb.Html(reader["Title"]);
                            currentStatus = Convert.ToString(reader["CurrentStatus"]);
                        }
                    }
                    if (!IsBidStageOrLater(currentStatus))
                    {
                        Response.Redirect("~/Pages/TenderDetails.aspx?id=" + TenderId.ToString(CultureInfo.InvariantCulture), false);
                        Context.ApplicationInstance.CompleteRequest();
                        return;
                    }
                    BindTechnical(connection);
                    BindFinancial(connection);
                    EvaluatePanel.Visible = HasAnyRole("TenderSection", "Admin");
                    SelectPanel.Visible = HasAnyRole("TenderCommittee", "Admin");
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Tender bids could not be loaded.", ex);
                ShowError("Bid information could not be loaded.");
            }
        }

        private void BindTechnical(SqlConnection connection)
        {
            using (SqlCommand command = new SqlCommand(
                @"SELECT b.TechnicalBidId,b.ContractorId,c.Name AS ContractorName,b.DocumentPath,b.Status,b.Remarks,b.SubmittedDate
                  FROM dbo.TechnicalBid b INNER JOIN dbo.Contractors c ON c.ContractorId=b.ContractorId
                  WHERE b.TenderId=@TenderId ORDER BY b.SubmittedDate;", connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                DataTable table = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(command)) adapter.Fill(table);
                TechnicalGrid.DataSource = table;
                TechnicalGrid.DataBind();
                TechnicalBidSelect.Items.Clear();
                TechnicalBidSelect.Items.Add(new ListItem("Select submitted bid", ""));
                foreach (DataRow row in table.Rows)
                {
                    if (String.Equals(Convert.ToString(row["Status"]), "Submitted", StringComparison.OrdinalIgnoreCase))
                    {
                        TechnicalBidSelect.Items.Add(new ListItem(
                            Convert.ToString(row["ContractorName"]) + " (bid " + Convert.ToString(row["TechnicalBidId"]) + ")",
                            Convert.ToString(row["TechnicalBidId"], CultureInfo.InvariantCulture)));
                    }
                }
            }
        }

        private void BindFinancial(SqlConnection connection)
        {
            using (SqlCommand command = new SqlCommand(
                @"SELECT fb.FinancialBidId,fb.ContractorId,c.Name AS ContractorName,fb.BidAmount,
                         RANK() OVER (ORDER BY fb.BidAmount ASC) AS Rank,fb.Status
                  FROM dbo.FinancialBid fb INNER JOIN dbo.Contractors c ON c.ContractorId=fb.ContractorId
                  WHERE fb.TenderId=@TenderId ORDER BY fb.BidAmount ASC;", connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                DataTable table = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(command)) adapter.Fill(table);
                FinancialGrid.DataSource = table;
                FinancialGrid.DataBind();
                FinancialBidSelect.Items.Clear();
                FinancialBidSelect.Items.Add(new ListItem("Select bidder", ""));
                foreach (DataRow row in table.Rows)
                {
                    FinancialBidSelect.Items.Add(new ListItem(
                        "L" + Convert.ToString(row["Rank"], CultureInfo.InvariantCulture) + " · " +
                        Convert.ToString(row["ContractorName"]) + " · " +
                        Convert.ToDecimal(row["BidAmount"]).ToString("C", CultureInfo.CurrentCulture),
                        Convert.ToString(row["ContractorId"], CultureInfo.InvariantCulture)));
                }
            }
        }

        protected void EvaluateButton_Click(object sender, EventArgs e)
        {
            int bidId;
            if (!HasAnyRole("TenderSection", "Admin") || !Int32.TryParse(TechnicalBidSelect.SelectedValue, out bidId) ||
                (TechnicalStatus.SelectedValue != "Qualified" && TechnicalStatus.SelectedValue != "Disqualified") ||
                TechnicalRemarks.Text.Length > 1000)
            {
                ShowError("Select a submitted bid, a valid evaluation decision, and remarks of at most 1,000 characters.");
                return;
            }
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                using (SqlCommand command = new SqlCommand(
                    @"UPDATE dbo.TechnicalBid SET Status=@Status,Remarks=@Remarks,EvaluatedBy=@UserId,EvaluatedDate=SYSUTCDATETIME()
                      WHERE TechnicalBidId=@BidId AND TenderId=@TenderId AND Status=N'Submitted';", connection, transaction))
                {
                    AddText(command, "@Status", SqlDbType.NVarChar, TechnicalStatus.SelectedValue, 30);
                    AddText(command, "@Remarks", SqlDbType.NVarChar, TechnicalRemarks.Text, 1000);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                    ModuleDb.AddParameter(command, "@BidId", SqlDbType.Int, bidId);
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("The bid is no longer awaiting evaluation.");
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "EvaluateTechnicalBid", "TechnicalBid", bidId, TechnicalStatus.SelectedValue, Request.UserHostAddress);
                    transaction.Commit();
                }
                Message.Text = "<div class=\"message success\">Technical bid evaluated.</div>";
                LoadPage();
            }
            catch (SqlException ex) { Trace.Warn("DepartmentModule", "Technical bid evaluation failed.", ex); ShowError("The technical bid could not be updated."); }
            catch (InvalidOperationException ex) { ShowError(ex.Message); }
        }

        protected void SelectContractorButton_Click(object sender, EventArgs e)
        {
            int contractorId;
            if (!HasAnyRole("TenderCommittee", "Admin") || !Int32.TryParse(FinancialBidSelect.SelectedValue, out contractorId) || SelectionRemarks.Text.Length > 1000)
            {
                ShowError("Select a bidder and enter no more than 1,000 characters of remarks.");
                return;
            }
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    using (SqlCommand command = new SqlCommand(
                        @"UPDATE dbo.FinancialBid SET Status=CASE WHEN ContractorId=@ContractorId THEN N'Selected' ELSE N'Rejected' END
                          WHERE TenderId=@TenderId;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int, contractorId);
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        if (command.ExecuteNonQuery() == 0) throw new InvalidOperationException("No financial bids are available for this tender.");
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "SelectContractor", "FinancialBid", TenderId, SelectionRemarks.Text, Request.UserHostAddress);
                    transaction.Commit();
                }
                Message.Text = "<div class=\"message success\">Contractor recommendation recorded.</div>";
                LoadPage();
            }
            catch (SqlException ex) { Trace.Warn("DepartmentModule", "Financial bid selection failed.", ex); ShowError("The contractor recommendation could not be saved."); }
            catch (InvalidOperationException ex) { ShowError(ex.Message); }
        }

        private void ShowError(string text)
        {
            Message.Text = "<div class=\"message\">" + HttpUtility.HtmlEncode(text) + "</div>";
        }

        private static bool IsBidStageOrLater(string status)
        {
            string[] bidStatuses =
            {
                "Tender Published", "Technical Bid Opening", "Technical Evaluation",
                "Technical Evaluation Approved", "Financial Bid Opening", "CAFO Rate Opinion",
                "Financial Evaluation", "MCA Opinion", "Tender Committee Recommendation",
                "Final Approval", "Agreement Completed", "Work Order Generated", "Work Started",
                "Measurement Book Updated", "Bill Submitted", "Bill Approved", "Payment Completed"
            };
            return Array.IndexOf(bidStatuses, status) >= 0;
        }
    }
}
