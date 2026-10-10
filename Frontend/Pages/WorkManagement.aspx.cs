using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class WorkManagement : ModulePage
    {
        protected Literal TenderNumber, TenderTitle, Message;
        protected HyperLink BackLink;
        protected TextBox AgreementNo, AgreementDate, AgreementDocument, OrderNo, IssueDate, OrderAmount, Progress, ProgressRemarks,
            MeasurementDescription, MeasurementQuantity, PaymentAmount, TransactionNo;
        protected DropDownList AgreementContractor, OrderContractor, BillSelect;
        protected Button CreateAgreementButton, CreateWorkOrderButton, UpdateProgressButton, CreateMeasurementButton, ProcessPaymentButton;
        protected GridView AgreementGrid, WorkOrderGrid, ProgressGrid, MeasurementGrid, BillsGrid, PaymentsGrid;
        protected Panel PaymentPanel;
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
                    using (SqlCommand command = new SqlCommand("SELECT TenderNumber,Title FROM dbo.Tender WHERE TenderId=@TenderId AND IsDeleted=0;", connection))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read()) { Response.Redirect("~/Pages/Tenders.aspx", false); Context.ApplicationInstance.CompleteRequest(); return; }
                            TenderNumber.Text = ModuleDb.Html(reader["TenderNumber"]);
                            TenderTitle.Text = ModuleDb.Html(reader["Title"]);
                        }
                    }
                    BackLink.NavigateUrl = "~/Pages/TenderDetails.aspx?id=" + TenderId.ToString(CultureInfo.InvariantCulture);
                    if (!IsPostBack) LoadContractors(connection);
                    BindGrid(connection, AgreementGrid,
                        @"SELECT a.AgreementNo,a.AgreementDate,a.DocumentPath,c.Name AS ContractorName FROM dbo.Agreement a INNER JOIN dbo.Contractors c ON c.ContractorId=a.ContractorId WHERE a.TenderId=@TenderId ORDER BY a.CreatedDate DESC;");
                    BindGrid(connection, WorkOrderGrid,
                        @"SELECT w.OrderNo,w.IssueDate,w.Amount,c.Name AS ContractorName FROM dbo.WorkOrder w INNER JOIN dbo.Contractors c ON c.ContractorId=w.ContractorId WHERE w.TenderId=@TenderId ORDER BY w.CreatedDate DESC;");
                    BindGrid(connection, ProgressGrid,
                        @"SELECT x.ProgressPercentage,x.Remarks,x.UpdatedDate,u.FullName AS UpdatedByName FROM dbo.WorkExecution x LEFT JOIN dbo.Users u ON u.UserId=x.UpdatedBy WHERE x.TenderId=@TenderId ORDER BY x.UpdatedDate DESC;");
                    BindGrid(connection, MeasurementGrid,
                        @"SELECT m.Description,m.Quantity,m.MeasurementDate,m.ApprovalStatus,u.FullName AS RecordedByName FROM dbo.MeasurementBook m LEFT JOIN dbo.Users u ON u.UserId=m.RecordedBy WHERE m.TenderId=@TenderId ORDER BY m.MeasurementDate DESC;");
                    BindGrid(connection, BillsGrid,
                        @"SELECT b.BillId,b.BillNo,b.Amount,b.Status,b.SubmittedDate,c.Name AS ContractorName FROM dbo.Bills b INNER JOIN dbo.Contractors c ON c.ContractorId=b.ContractorId WHERE b.TenderId=@TenderId ORDER BY b.SubmittedDate DESC;");
                    BindGrid(connection, PaymentsGrid,
                        @"SELECT p.Amount,p.TransactionNo,p.PaymentDate,b.BillNo FROM dbo.Payments p INNER JOIN dbo.Bills b ON b.BillId=p.BillId WHERE b.TenderId=@TenderId ORDER BY p.PaymentDate DESC;");
                    if (!IsPostBack) LoadApprovedBills(connection);
                    PaymentPanel.Visible = HasAnyRole("CAFOOfficer", "Admin");
                }
                CreateAgreementButton.Visible = HasAnyRole("DepartmentOfficer", "Admin");
                CreateWorkOrderButton.Visible = HasAnyRole("DepartmentOfficer", "Admin");
                UpdateProgressButton.Visible = HasAnyRole("DepartmentOfficer", "Admin");
                CreateMeasurementButton.Visible = HasAnyRole("DepartmentOfficer", "Admin");
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Work-management data could not be loaded.", ex);
                ShowError("Work-management data could not be loaded.");
            }
        }

        private void LoadContractors(SqlConnection connection)
        {
            using (SqlCommand command = new SqlCommand("SELECT ContractorId,Name FROM dbo.Contractors WHERE IsActive=1 ORDER BY Name;", connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                AgreementContractor.Items.Clear();
                OrderContractor.Items.Clear();
                AgreementContractor.Items.Add(new ListItem("Select contractor", ""));
                OrderContractor.Items.Add(new ListItem("Select contractor", ""));
                while (reader.Read())
                {
                    string id = reader.GetInt32(0).ToString(CultureInfo.InvariantCulture);
                    AgreementContractor.Items.Add(new ListItem(reader.GetString(1), id));
                    OrderContractor.Items.Add(new ListItem(reader.GetString(1), id));
                }
            }
        }

        private void LoadApprovedBills(SqlConnection connection)
        {
            using (SqlCommand command = new SqlCommand("SELECT BillId,BillNo FROM dbo.Bills WHERE TenderId=@TenderId AND Status=N'Approved' ORDER BY SubmittedDate;", connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    BillSelect.Items.Clear();
                    BillSelect.Items.Add(new ListItem("Select approved bill", ""));
                    while (reader.Read()) BillSelect.Items.Add(new ListItem(reader.GetString(1), reader.GetInt32(0).ToString(CultureInfo.InvariantCulture)));
                }
            }
        }

        private void BindGrid(SqlConnection connection, GridView grid, string sql)
        {
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                DataTable table = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(command)) adapter.Fill(table);
                grid.DataSource = table;
                grid.DataBind();
            }
        }

        protected void CreateAgreementButton_Click(object sender, EventArgs e)
        {
            int contractorId; DateTime date;
            if (!HasAnyRole("DepartmentOfficer", "Admin") || String.IsNullOrWhiteSpace(AgreementNo.Text) ||
                AgreementNo.Text.Trim().Length > 50 || !Int32.TryParse(AgreementContractor.SelectedValue, out contractorId) ||
                !DateTime.TryParse(AgreementDate.Text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date) ||
                AgreementDocument.Text.Length > 400)
            {
                ShowError("Provide a valid agreement number, date, contractor, and document path (maximum 400 characters)."); return;
            }
            ExecuteInsert(
                @"INSERT INTO dbo.Agreement (TenderId,ContractorId,AgreementNo,AgreementDate,DocumentPath)
                  VALUES (@TenderId,@ContractorId,@Number,@Date,@Document); SELECT CAST(SCOPE_IDENTITY() AS int);",
                "CreateAgreement", "Agreement", command =>
                {
                    ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int, contractorId);
                    AddText(command, "@Number", SqlDbType.NVarChar, AgreementNo.Text, 50);
                    ModuleDb.AddParameter(command, "@Date", SqlDbType.DateTime2, date);
                    AddText(command, "@Document", SqlDbType.NVarChar, AgreementDocument.Text, 400);
                });
        }

        protected void CreateWorkOrderButton_Click(object sender, EventArgs e)
        {
            int contractorId; DateTime date; decimal amount;
            if (!HasAnyRole("DepartmentOfficer", "Admin") || String.IsNullOrWhiteSpace(OrderNo.Text) || OrderNo.Text.Trim().Length > 50 ||
                !Int32.TryParse(OrderContractor.SelectedValue, out contractorId) ||
                !DateTime.TryParse(IssueDate.Text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date) ||
                !Decimal.TryParse(OrderAmount.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) || amount <= 0)
            {
                ShowError("Provide a valid work order number, issue date, contractor, and positive amount."); return;
            }
            ExecuteInsert(
                @"INSERT INTO dbo.WorkOrder (TenderId,ContractorId,OrderNo,IssueDate,Amount)
                  VALUES (@TenderId,@ContractorId,@Number,@Date,@Amount); SELECT CAST(SCOPE_IDENTITY() AS int);",
                "CreateWorkOrder", "WorkOrder", command =>
                {
                    ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int, contractorId);
                    AddText(command, "@Number", SqlDbType.NVarChar, OrderNo.Text, 50);
                    ModuleDb.AddParameter(command, "@Date", SqlDbType.DateTime2, date);
                    ModuleDb.AddParameter(command, "@Amount", SqlDbType.Decimal, amount);
                });
        }

        protected void UpdateProgressButton_Click(object sender, EventArgs e)
        {
            decimal progress;
            if (!HasAnyRole("DepartmentOfficer", "Admin") || !Decimal.TryParse(Progress.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out progress) ||
                progress < 0 || progress > 100 || ProgressRemarks.Text.Length > 1000)
            {
                ShowError("Progress percentage must be between 0 and 100. Remarks must not exceed 1,000 characters."); return;
            }
            ExecuteInsert(
                @"INSERT INTO dbo.WorkExecution (TenderId,ProgressPercentage,Remarks,UpdatedBy)
                  VALUES (@TenderId,@Progress,@Remarks,@UserId); SELECT CAST(SCOPE_IDENTITY() AS int);",
                "UpdateProgress", "WorkExecution", command =>
                {
                    ModuleDb.AddParameter(command, "@Progress", SqlDbType.Decimal, progress);
                    AddText(command, "@Remarks", SqlDbType.NVarChar, ProgressRemarks.Text, 1000);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                });
        }

        protected void CreateMeasurementButton_Click(object sender, EventArgs e)
        {
            decimal quantity;
            if (!HasAnyRole("DepartmentOfficer", "Admin") || String.IsNullOrWhiteSpace(MeasurementDescription.Text) ||
                MeasurementDescription.Text.Trim().Length > 500 ||
                !Decimal.TryParse(MeasurementQuantity.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out quantity) || quantity <= 0)
            {
                ShowError("Enter a description (maximum 500 characters) and a positive quantity."); return;
            }
            ExecuteInsert(
                @"INSERT INTO dbo.MeasurementBook (TenderId,Description,Quantity,RecordedBy,ApprovalStatus)
                  VALUES (@TenderId,@Description,@Quantity,@UserId,N'Pending'); SELECT CAST(SCOPE_IDENTITY() AS int);",
                "CreateMeasurementBook", "MeasurementBook", command =>
                {
                    AddText(command, "@Description", SqlDbType.NVarChar, MeasurementDescription.Text, 500);
                    ModuleDb.AddParameter(command, "@Quantity", SqlDbType.Decimal, quantity);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                });
        }

        private void ExecuteInsert(string sql, string action, string entity, Action<SqlCommand> setParameters)
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                using (SqlCommand command = new SqlCommand(sql, connection, transaction))
                {
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    setParameters(command);
                    int id = Convert.ToInt32(command.ExecuteScalar());
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, action, entity, id, null, Request.UserHostAddress);
                    transaction.Commit();
                }
                Message.Text = "<div class=\"message success\">Record saved.</div>";
                LoadPage();
            }
            catch (SqlException ex) { Trace.Warn("DepartmentModule", action + " failed.", ex); ShowError("The record could not be saved. Check for duplicate numbers and retry."); }
        }

        protected void Bill_Command(object sender, CommandEventArgs e)
        {
            int billId;
            if (!Int32.TryParse(Convert.ToString(e.CommandArgument), out billId) || billId <= 0) { ShowError("Invalid bill."); return; }
            if (e.CommandName == "ApproveBill" && HasAnyRole("CAFOOfficer", "Admin"))
            {
                UpdateBill(billId, "Approved");
            }
            else if (e.CommandName == "PayBill" && HasAnyRole("CAFOOfficer", "Admin"))
            {
                BillSelect.SelectedValue = billId.ToString(CultureInfo.InvariantCulture);
                PaymentAmount.Focus();
            }
            else ShowError("Your role is not authorized for this bill action.");
        }

        private void UpdateBill(int billId, string status)
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                using (SqlCommand command = new SqlCommand(
                    @"UPDATE dbo.Bills SET Status=@Status,ApprovedBy=@UserId,ApprovedDate=SYSUTCDATETIME()
                      WHERE BillId=@BillId AND TenderId=@TenderId;", connection, transaction))
                {
                    AddText(command, "@Status", SqlDbType.NVarChar, status, 30);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                    ModuleDb.AddParameter(command, "@BillId", SqlDbType.Int, billId);
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Bill not found.");
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "ApproveBill", "Bill", billId, status, Request.UserHostAddress);
                    transaction.Commit();
                }
                Message.Text = "<div class=\"message success\">Bill approved.</div>";
                LoadPage();
            }
            catch (SqlException ex) { Trace.Warn("DepartmentModule", "Bill approval failed.", ex); ShowError("The bill could not be approved."); }
            catch (InvalidOperationException ex) { ShowError(ex.Message); }
        }

        protected void ProcessPaymentButton_Click(object sender, EventArgs e)
        {
            int billId; decimal amount;
            if (!HasAnyRole("CAFOOfficer", "Admin") || !Int32.TryParse(BillSelect.SelectedValue, out billId) ||
                !Decimal.TryParse(PaymentAmount.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) || amount <= 0 ||
                String.IsNullOrWhiteSpace(TransactionNo.Text) || TransactionNo.Text.Trim().Length > 100)
            {
                ShowError("Select an approved bill, enter a positive amount, and provide a transaction number of at most 100 characters."); return;
            }
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    using (SqlCommand command = new SqlCommand(
                        @"INSERT INTO dbo.Payments (BillId,Amount,TransactionNo,ProcessedBy)
                          SELECT BillId,@Amount,@TransactionNo,@UserId FROM dbo.Bills
                          WHERE BillId=@BillId AND TenderId=@TenderId AND Status=N'Approved';", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@Amount", SqlDbType.Decimal, amount);
                        AddText(command, "@TransactionNo", SqlDbType.NVarChar, TransactionNo.Text, 100);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(command, "@BillId", SqlDbType.Int, billId);
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Only an approved bill can be paid.");
                    }
                    using (SqlCommand command = new SqlCommand(
                        "UPDATE dbo.Bills SET Status=N'Paid' WHERE BillId=@BillId AND TenderId=@TenderId AND Status=N'Approved';", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@BillId", SqlDbType.Int, billId);
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Bill status changed before payment.");
                    }
                    int paymentId;
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT TOP (1) PaymentId FROM dbo.Payments
                          WHERE BillId=@BillId AND TransactionNo=@TransactionNo;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@BillId", SqlDbType.Int, billId);
                        AddText(command, "@TransactionNo", SqlDbType.NVarChar, TransactionNo.Text, 100);
                        paymentId = Convert.ToInt32(command.ExecuteScalar());
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "ProcessPayment", "Payment", paymentId, TransactionNo.Text, Request.UserHostAddress);
                    transaction.Commit();
                }
                Message.Text = "<div class=\"message success\">Payment processed.</div>";
                LoadPage();
            }
            catch (SqlException ex) { Trace.Warn("DepartmentModule", "Payment processing failed.", ex); ShowError("The payment could not be processed. Check the transaction number and retry."); }
            catch (InvalidOperationException ex) { ShowError(ex.Message); }
        }

        private void ShowError(string text)
        {
            Message.Text = "<div class=\"message\">" + HttpUtility.HtmlEncode(text) + "</div>";
        }
    }
}
