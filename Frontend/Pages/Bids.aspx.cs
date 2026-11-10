using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class Bids : ModulePage
    {
        protected Literal TenderNumber, TenderTitle, Message;
        protected HtmlGenericControl TechnicalBidsSection, FinancialBidsSection;
        protected HyperLink BackLink;
        protected GridView TechnicalGrid, FinancialGrid;
        protected Panel EvaluatePanel, SelectPanel, TechnicalSubmissionPanel, FinancialSubmissionPanel;
        protected DropDownList TechnicalBidSelect, TechnicalStatus, FinancialBidSelect, TechnicalContractorSelect, FinancialContractorSelect;
        protected TextBox TechnicalRemarks, SelectionRemarks, TechnicalDocumentPathText, FinancialBidAmountText;
        protected Button EvaluateButton, SelectContractorButton, SubmitTechnicalButton, SubmitFinancialButton;
        protected Literal ContractorProfileMessage;
        protected System.Web.UI.HtmlControls.HtmlGenericControl TechnicalContractorField, FinancialContractorField;
        private int TenderId { get { return QueryId("tenderId"); } }
        private bool IsReviewer { get { return HasAnyRole("TenderSection", "TenderCommittee", "CAFOOfficer", "Admin"); } }
        private bool IsContractor { get { return HasAnyRole("Contractor"); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (TenderId <= 0) { Response.Redirect("~/Pages/Tenders.aspx", false); Context.ApplicationInstance.CompleteRequest(); return; }
            if (!IsReviewer && !IsContractor)
            {
                Response.StatusCode = 403;
                Response.TrySkipIisCustomErrors = true;
                Response.Redirect("~/Pages/Error.aspx?code=403", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            if (!IsPostBack) LoadPage();
        }

        private void LoadPage()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    string currentStatus;
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT t.TenderNumber,t.Title,t.CurrentStatus
                          FROM dbo.Tender t
                          WHERE t.TenderId=@TenderId AND t.IsDeleted=0
                            AND (@Role=N'Admin' OR
                                 (NOT EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                             WHERE scopeUser.UserId=@UserId AND scopeUser.DepartmentId IS NOT NULL)
                                  OR EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                            WHERE scopeUser.UserId=@UserId AND scopeUser.DepartmentId=t.DepartmentId)));",
                        connection))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        AddText(command, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
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
                    bool openingComplete;
                    using (SqlCommand command = new SqlCommand(
                        "SELECT COUNT(1) FROM dbo.TenderBidOpening WHERE TenderId=@TenderId;", connection))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        openingComplete = Convert.ToInt32(command.ExecuteScalar()) == 1;
                    }
                    bool financialOpening = IsFinancialOpeningOrLater(currentStatus);
                    bool isAuthorizedReviewer = IsReviewer && openingComplete;
                    bool canSubmit = IsContractor && IsBidWindowOpen(connection, null);
                    TechnicalBidsSection.Visible = (IsContractor || isAuthorizedReviewer) &&
                        (!IsReviewer || openingComplete);
                    FinancialBidsSection.Visible = IsContractor && canSubmit ||
                        (isAuthorizedReviewer && financialOpening);
                    if (IsReviewer && !openingComplete)
                    {
                        Message.Text = "<div class=\"message\">Bid contents remain confidential until the authorized bid opening has been recorded.</div>";
                    }
                    else if (IsContractor && !canSubmit)
                    {
                        Message.Text = "<div class=\"message\">Bid submission is available only during the approved publication period.</div>";
                    }
                    BindTechnical(connection);
                    if (financialOpening && isAuthorizedReviewer || IsContractor)
                        BindFinancial(connection);
                    else
                    {
                        FinancialGrid.DataSource = new DataTable();
                        FinancialGrid.DataBind();
                    }
                    EvaluatePanel.Visible = HasAnyRole("TenderSection", "Admin") &&
                        openingComplete && String.Equals(currentStatus, "Technical Evaluation", StringComparison.OrdinalIgnoreCase);
                    SelectPanel.Visible = HasAnyRole("TenderCommittee", "Admin") && openingComplete && financialOpening;
                    ConfigureSubmissionPanels(connection, canSubmit);
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
            string bidderScope = IsContractor ? " AND b.ContractorId=@ContractorId" : String.Empty;
            bool mayReadAll = IsReviewer && IsBidOpeningRecorded(connection);
            if (!mayReadAll && !IsContractor)
            {
                TechnicalGrid.DataSource = new DataTable();
                TechnicalGrid.DataBind();
                TechnicalBidSelect.Items.Clear();
                TechnicalBidSelect.Items.Add(new ListItem("Select submitted bid", ""));
                return;
            }
            using (SqlCommand command = new SqlCommand(
                @"SELECT b.TechnicalBidId,b.ContractorId,c.Name AS ContractorName,b.DocumentPath,b.Status,b.Remarks,b.SubmittedDate
                  FROM dbo.TechnicalBid b INNER JOIN dbo.Contractors c ON c.ContractorId=b.ContractorId
                  WHERE b.TenderId=@TenderId" + bidderScope + " ORDER BY b.SubmittedDate;", connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                if (IsContractor)
                {
                    int? contractorId = GetCurrentContractorId(connection);
                    ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int,
                        contractorId.HasValue ? (object)contractorId.Value : -1);
                }
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
            string bidderScope = IsContractor ? " AND fb.ContractorId=@ContractorId" : String.Empty;
            using (SqlCommand command = new SqlCommand(
                @"SELECT fb.FinancialBidId,fb.ContractorId,c.Name AS ContractorName,fb.BidAmount,
                         RANK() OVER (ORDER BY fb.BidAmount ASC) AS Rank,fb.Status
                  FROM dbo.FinancialBid fb INNER JOIN dbo.Contractors c ON c.ContractorId=fb.ContractorId
                  WHERE fb.TenderId=@TenderId" + bidderScope + " ORDER BY fb.BidAmount ASC;", connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                if (IsContractor)
                {
                    int? contractorId = GetCurrentContractorId(connection);
                    ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int,
                        contractorId.HasValue ? (object)contractorId.Value : -1);
                }
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

        private void ConfigureSubmissionPanels(SqlConnection connection, bool canSubmit)
        {
            FinancialBidAmountText.Attributes["step"] = "0.01";
            FinancialBidAmountText.Attributes["min"] = "0.01";
            bool isAdmin = HasAnyRole("Admin");
            TechnicalSubmissionPanel.Visible = (IsContractor || isAdmin) && canSubmit;
            FinancialSubmissionPanel.Visible = (IsContractor || isAdmin) && canSubmit;
            TechnicalContractorField.Visible = isAdmin;
            FinancialContractorField.Visible = isAdmin;

            if (isAdmin)
            {
                BindContractors(connection, TechnicalContractorSelect);
                BindContractors(connection, FinancialContractorSelect);
                return;
            }

            if (!IsContractor)
            {
                return;
            }

            int? contractorId = GetCurrentContractorId(connection);
            if (!contractorId.HasValue)
            {
                ContractorProfileMessage.Text = "<div class=\"message\">No active contractor profile is linked to this account. Contact an administrator to link your contractor record.</div>";
                SubmitTechnicalButton.Enabled = false;
                SubmitFinancialButton.Enabled = false;
                return;
            }

            string technicalStatus;
            using (SqlCommand command = new SqlCommand(
                "SELECT Status FROM dbo.TechnicalBid WHERE TenderId=@TenderId AND ContractorId=@ContractorId;", connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int, contractorId.Value);
                technicalStatus = Convert.ToString(command.ExecuteScalar());
            }

            if (!String.IsNullOrEmpty(technicalStatus))
            {
                SubmitTechnicalButton.Enabled = false;
                ContractorProfileMessage.Text = "<div class=\"message\">Your technical bid status is " + HttpUtility.HtmlEncode(technicalStatus) + ".</div>";
            }

            using (SqlCommand command = new SqlCommand(
                "SELECT COUNT(1) FROM dbo.FinancialBid WHERE TenderId=@TenderId AND ContractorId=@ContractorId;", connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int, contractorId.Value);
                if (Convert.ToInt32(command.ExecuteScalar()) > 0)
                {
                    SubmitFinancialButton.Enabled = false;
                    ContractorProfileMessage.Text = "<div class=\"message\">Your financial bid has already been submitted.</div>";
                }
            }
        }

        private static void BindContractors(SqlConnection connection, DropDownList list)
        {
            list.Items.Clear();
            list.Items.Add(new ListItem("Select contractor", ""));
            using (SqlCommand command = new SqlCommand(
                "SELECT ContractorId,Name FROM dbo.Contractors WHERE IsActive=1 ORDER BY Name;", connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Items.Add(new ListItem(
                        Convert.ToString(reader["Name"]),
                        Convert.ToString(reader["ContractorId"], CultureInfo.InvariantCulture)));
                }
            }
        }

        private int? GetCurrentContractorId(SqlConnection connection)
        {
            using (SqlCommand command = new SqlCommand(
                "SELECT ContractorId FROM dbo.Contractors WHERE UserId=@UserId AND IsActive=1;", connection))
            {
                ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                object value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
            }
        }

        private int? ResolveSubmissionContractorId(SqlConnection connection, DropDownList contractorList)
        {
            if (HasAnyRole("Contractor"))
            {
                return GetCurrentContractorId(connection);
            }

            int contractorId;
            if (!HasAnyRole("Admin") || !Int32.TryParse(contractorList.SelectedValue, out contractorId) || contractorId <= 0)
            {
                return null;
            }

            using (SqlCommand command = new SqlCommand(
                "SELECT COUNT(1) FROM dbo.Contractors WHERE ContractorId=@ContractorId AND IsActive=1;", connection))
            {
                ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int, contractorId);
                return Convert.ToInt32(command.ExecuteScalar()) == 1 ? (int?)contractorId : null;
            }
        }

        protected void SubmitTechnicalButton_Click(object sender, EventArgs e)
        {
            if (!HasAnyRole("Contractor", "Admin") || TechnicalDocumentPathText.Text.Trim().Length > 400)
            {
                ShowError("A contractor or administrator must submit the bid, and the document path cannot exceed 400 characters.");
                return;
            }

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    int? contractorId = ResolveSubmissionContractorId(connection, TechnicalContractorSelect);
                    if (!contractorId.HasValue)
                    {
                        ShowError("An active contractor profile is required to submit a technical bid.");
                        return;
                    }

                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        EnsureBidWindowOpen(connection, transaction);
                        using (SqlCommand command = new SqlCommand(
                            @"INSERT INTO dbo.TechnicalBid (TenderId,ContractorId,DocumentPath,Status,SubmittedDate)
                              VALUES (@TenderId,@ContractorId,@DocumentPath,N'Submitted',SYSUTCDATETIME());", connection, transaction))
                        {
                            ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                            ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int, contractorId.Value);
                            AddText(command, "@DocumentPath", SqlDbType.NVarChar, TechnicalDocumentPathText.Text, 400);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                }

                Message.Text = "<div class=\"message success\">Technical bid submitted.</div>";
                LoadPage();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Technical bid submission failed.", ex);
                ShowError(ex.Number == 2601 || ex.Number == 2627
                    ? "A technical bid has already been submitted for this contractor and tender."
                    : "The technical bid could not be submitted.");
            }
            catch (InvalidOperationException ex) { ShowError(ex.Message); }
        }

        protected void SubmitFinancialButton_Click(object sender, EventArgs e)
        {
            decimal bidAmount;
            if (!HasAnyRole("Contractor", "Admin") ||
                !Decimal.TryParse(FinancialBidAmountText.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out bidAmount) ||
                bidAmount < 0.01m || bidAmount > 9999999999999999.99m || Decimal.Round(bidAmount, 2) != bidAmount)
            {
                ShowError("Enter a valid bid amount greater than zero with no more than two decimal places.");
                return;
            }

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    int? contractorId = ResolveSubmissionContractorId(connection, FinancialContractorSelect);
                    if (!contractorId.HasValue)
                    {
                        ShowError("An active contractor profile is required to submit a financial bid.");
                        return;
                    }

                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        EnsureBidWindowOpen(connection, transaction);

                        using (SqlCommand command = new SqlCommand(
                            @"INSERT INTO dbo.FinancialBid (TenderId,ContractorId,BidAmount,Status,SubmittedDate)
                              VALUES (@TenderId,@ContractorId,@BidAmount,N'Submitted',SYSUTCDATETIME());", connection, transaction))
                        {
                            ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                            ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int, contractorId.Value);
                            ModuleDb.AddParameter(command, "@BidAmount", SqlDbType.Decimal, bidAmount);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                }

                Message.Text = "<div class=\"message success\">Financial bid submitted.</div>";
                LoadPage();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Financial bid submission failed.", ex);
                ShowError(ex.Number == 2601 || ex.Number == 2627
                    ? "A financial bid has already been submitted for this contractor and tender."
                    : "The financial bid could not be submitted.");
            }
            catch (InvalidOperationException ex) { ShowError(ex.Message); }
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
                      WHERE TechnicalBidId=@BidId AND TenderId=@TenderId AND Status=N'Submitted'
                        AND EXISTS(SELECT 1 FROM dbo.Tender t WHERE t.TenderId=@TenderId AND t.CurrentStatus=N'Technical Evaluation'
                                   AND (@Role=N'Admin' OR
                                        (NOT EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                                    WHERE scopeUser.UserId=@UserId AND scopeUser.DepartmentId IS NOT NULL)
                                         OR EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                                   WHERE scopeUser.UserId=@UserId AND scopeUser.DepartmentId=t.DepartmentId))))
                        AND EXISTS(SELECT 1 FROM dbo.TenderBidOpening o WHERE o.TenderId=@TenderId);", connection, transaction))
                {
                    AddText(command, "@Status", SqlDbType.NVarChar, TechnicalStatus.SelectedValue, 30);
                    AddText(command, "@Remarks", SqlDbType.NVarChar, TechnicalRemarks.Text, 1000);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                    AddText(command, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
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
                          WHERE TenderId=@TenderId
                            AND EXISTS(SELECT 1 FROM dbo.Tender t WHERE t.TenderId=@TenderId
                                       AND t.CurrentStatus IN (N'Financial Bid Opening',N'CAFO Rate Opinion',
                                           N'Financial Evaluation',N'MCA Opinion',N'Tender Committee Recommendation',
                                           N'Final Approval',N'Agreement Completed')
                                       AND (@Role=N'Admin' OR
                                            (NOT EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                                        WHERE scopeUser.UserId=@UserId AND scopeUser.DepartmentId IS NOT NULL)
                                             OR EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                                       WHERE scopeUser.UserId=@UserId AND scopeUser.DepartmentId=t.DepartmentId))));", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int, contractorId);
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        AddText(command, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
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

        private bool IsBidWindowOpen(SqlConnection connection, SqlTransaction transaction)
        {
            using (SqlCommand command = new SqlCommand(
                @"SELECT COUNT(1)
                  FROM dbo.Tender t
                  INNER JOIN dbo.TenderPublicationSchedule s ON s.TenderId=t.TenderId
                  WHERE t.TenderId=@TenderId AND t.IsDeleted=0 AND t.CurrentStatus=N'Tender Published'
                    AND s.PublicationStartUtc<=SYSUTCDATETIME()
                    AND s.CurrentClosingUtc>SYSUTCDATETIME();", connection, transaction))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                return Convert.ToInt32(command.ExecuteScalar()) == 1;
            }
        }

        private void EnsureBidWindowOpen(SqlConnection connection, SqlTransaction transaction)
        {
            if (!IsBidWindowOpen(connection, transaction))
                throw new InvalidOperationException("Bid submission is available only during the approved publication period.");
        }

        private bool IsBidOpeningRecorded(SqlConnection connection)
        {
            using (SqlCommand command = new SqlCommand(
                "SELECT COUNT(1) FROM dbo.TenderBidOpening WHERE TenderId=@TenderId;", connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                return Convert.ToInt32(command.ExecuteScalar()) == 1;
            }
        }

        private static bool IsFinancialOpeningOrLater(string status)
        {
            string[] statuses =
            {
                "Financial Bid Opening", "CAFO Rate Opinion", "Financial Evaluation", "MCA Opinion",
                "Tender Committee Recommendation", "Final Approval", "Agreement Completed",
                "Work Order Generated", "Work Started", "Measurement Book Updated", "Bill Submitted",
                "Bill Approved", "Payment Completed"
            };
            return Array.IndexOf(statuses, status) >= 0;
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
