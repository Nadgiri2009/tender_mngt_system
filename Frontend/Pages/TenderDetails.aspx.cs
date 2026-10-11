using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class TenderDetails : ModulePage
    {
        protected Literal PageTitle, TenderTitle, TenderNumber, DepartmentName, Description, TenderType, EstimatedAmount;
        protected Literal SubmissionDeadline, CreatedBy, CreatedDate, CurrentStatus, Message;
        protected GridView BoqGrid, DocumentsGrid, HistoryGrid;
        protected Panel AddBoqPanel, UploadPanel;
        protected TextBox ItemName, ItemDescription, Quantity, Unit, Rate, Remarks;
        protected FileUpload DocumentFile;
        protected Button AddBoqButton, UploadButton, SubmitWorkflowButton, ApproveButton, RejectButton;
        protected Button DeleteTenderButton;
        protected HyperLink BidsLink, WorkLink, EditTenderLink;
        private int TenderId { get { return QueryId("id"); } }
        private string TenderStatus { get; set; }
        private bool IsDraft { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (TenderId <= 0)
            {
                Response.Redirect("~/Pages/Tenders.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            LoadTender();
        }

        private void LoadTender()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT t.TenderId,t.TenderNumber,t.Title,t.Description,t.TenderType,t.EstimatedAmount,t.CurrentStatus,
                                 t.SubmissionDeadline,t.CreatedDate,d.DepartmentName,u.FullName
                          FROM dbo.Tender t INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                          INNER JOIN dbo.Users u ON u.UserId=t.CreatedBy
                          WHERE t.TenderId=@TenderId AND t.IsDeleted=0;", connection))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                Response.Redirect("~/Pages/Tenders.aspx", false);
                                Context.ApplicationInstance.CompleteRequest();
                                return;
                            }
                            TenderNumber.Text = PageTitle.Text = ModuleDb.Html(reader["TenderNumber"]);
                            TenderTitle.Text = ModuleDb.Html(reader["Title"]);
                            DepartmentName.Text = ModuleDb.Html(reader["DepartmentName"]);
                            Description.Text = ModuleDb.Html(reader["Description"]);
                            TenderType.Text = ModuleDb.Html(reader["TenderType"]);
                            EstimatedAmount.Text = Convert.ToDecimal(reader["EstimatedAmount"]).ToString("N2", CultureInfo.CurrentCulture);
                            TenderStatus = Convert.ToString(reader["CurrentStatus"]);
                            CurrentStatus.Text = ModuleDb.Html(TenderStatus);
                            SubmissionDeadline.Text = reader["SubmissionDeadline"] == DBNull.Value ? "—" : Convert.ToDateTime(reader["SubmissionDeadline"]).ToString("dd MMM yyyy HH:mm", CultureInfo.CurrentCulture);
                            CreatedBy.Text = ModuleDb.Html(reader["FullName"]);
                            CreatedDate.Text = Convert.ToDateTime(reader["CreatedDate"]).ToString("dd MMM yyyy", CultureInfo.CurrentCulture);
                        }
                    }
                    BindChildTables(connection);
                }
                IsDraft = String.Equals(TenderStatus, "Draft", StringComparison.OrdinalIgnoreCase);
                EditTenderLink.Visible = IsDraft && HasAnyRole("Admin");
                EditTenderLink.NavigateUrl = "~/Pages/TenderForm.aspx?id=" + TenderId.ToString(CultureInfo.InvariantCulture);
                DeleteTenderButton.Visible = IsDraft && HasAnyRole("Admin");
                AddBoqPanel.Visible = IsDraft && HasAnyRole("DepartmentOfficer", "Admin");
                UploadPanel.Visible = HasAnyRole("DepartmentOfficer", "Admin");
                BoqGrid.Columns[6].Visible = AddBoqPanel.Visible;
                WorkflowTransition transition = WorkflowRules.ForStatus(TenderStatus);
                SubmitWorkflowButton.Visible = transition != null && transition.SubmitRoles.Length > 0 && WorkflowRules.RoleAllowed(transition.SubmitRoles, CurrentRole);
                ApproveButton.Visible = transition != null && transition.ApproverRoles.Length > 0 && WorkflowRules.RoleAllowed(transition.ApproverRoles, CurrentRole);
                RejectButton.Visible = ApproveButton.Visible;
                BidsLink.NavigateUrl = "~/Pages/Bids.aspx?tenderId=" + TenderId.ToString(CultureInfo.InvariantCulture);
                BidsLink.Visible = IsBidStageOrLater(TenderStatus);
                WorkLink.NavigateUrl = "~/Pages/WorkManagement.aspx?tenderId=" + TenderId.ToString(CultureInfo.InvariantCulture);
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Tender detail could not be loaded.", ex);
                Message.Text = "<div class=\"message\">Tender details could not be loaded. Please retry or contact support.</div>";
            }
        }

        private void BindChildTables(SqlConnection connection)
        {
            BindGrid(connection, BoqGrid,
                @"SELECT BOQId,ItemName,Description,Quantity,Unit,Rate,Amount FROM dbo.BOQ WHERE TenderId=@TenderId ORDER BY BOQId;");
            BindGrid(connection, DocumentsGrid,
                @"SELECT DocumentId,DocumentType,FileName,UploadedDate FROM dbo.TenderDocuments WHERE TenderId=@TenderId ORDER BY UploadedDate DESC;");
            BindGrid(connection, HistoryGrid,
                @"SELECT h.ApprovalStage,h.PreviousStatus,h.NewStatus,h.Status,h.RoleAtAction,h.Remarks,h.ApprovalDate,u.FullName AS ActionTakenBy
                  FROM dbo.ApprovalHistory h INNER JOIN dbo.Users u ON u.UserId=h.ApprovedBy
                  WHERE h.TenderId=@TenderId ORDER BY h.ApprovalDate;");
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

        protected void AddBoqButton_Click(object sender, EventArgs e)
        {
            if (!HasAnyRole("DepartmentOfficer", "Admin") || !String.Equals(TenderStatus, "Draft", StringComparison.OrdinalIgnoreCase))
            {
                ShowError("BOQ items can only be added to a draft tender by department staff.");
                return;
            }
            decimal quantity, rate;
            if (String.IsNullOrWhiteSpace(ItemName.Text) || ItemName.Text.Trim().Length > 200 ||
                ItemDescription.Text.Length > 500 || String.IsNullOrWhiteSpace(Unit.Text) || Unit.Text.Trim().Length > 20 ||
                !Decimal.TryParse(Quantity.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out quantity) || quantity <= 0 ||
                !Decimal.TryParse(Rate.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out rate) || rate <= 0)
            {
                ShowError("Enter an item name (maximum 200 characters), unit (maximum 20 characters), positive quantity and rate, and valid description.");
                return;
            }
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                using (SqlCommand command = new SqlCommand(
                    @"INSERT INTO dbo.BOQ (TenderId,ItemName,Description,Quantity,Unit,Rate)
                      VALUES (@TenderId,@ItemName,@Description,@Quantity,@Unit,@Rate);
                      SELECT CAST(SCOPE_IDENTITY() AS int);", connection, transaction))
                {
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    AddText(command, "@ItemName", SqlDbType.NVarChar, ItemName.Text, 200);
                    AddText(command, "@Description", SqlDbType.NVarChar, ItemDescription.Text, 500);
                    ModuleDb.AddParameter(command, "@Quantity", SqlDbType.Decimal, quantity);
                    AddText(command, "@Unit", SqlDbType.NVarChar, Unit.Text, 20);
                    ModuleDb.AddParameter(command, "@Rate", SqlDbType.Decimal, rate);
                    int boqId = Convert.ToInt32(command.ExecuteScalar());
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "Create", "BOQItem", boqId, ItemName.Text.Trim(), Request.UserHostAddress);
                    transaction.Commit();
                }
                Message.Text = "<div class=\"message success\">BOQ item added.</div>";
                LoadTender();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "BOQ item insert failed.", ex);
                ShowError("The BOQ item could not be saved.");
            }
        }

        protected void BoqGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "RemoveItem") return;
            int boqId;
            if (!Int32.TryParse(Convert.ToString(e.CommandArgument), out boqId) || boqId <= 0 ||
                !HasAnyRole("DepartmentOfficer", "Admin") || !String.Equals(TenderStatus, "Draft", StringComparison.OrdinalIgnoreCase))
            {
                ShowError("This BOQ item cannot be removed.");
                return;
            }
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    using (SqlCommand command = new SqlCommand("DELETE FROM dbo.BOQ WHERE BOQId=@BOQId AND TenderId=@TenderId;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@BOQId", SqlDbType.Int, boqId);
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("The requested BOQ item was not found.");
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "Delete", "BOQItem", boqId, null, Request.UserHostAddress);
                    transaction.Commit();
                }
                LoadTender();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "BOQ item delete failed.", ex);
                ShowError("The BOQ item could not be removed.");
            }
        }

        protected void UploadButton_Click(object sender, EventArgs e)
        {
            if (!HasAnyRole("DepartmentOfficer", "Admin") || !DocumentFile.HasFile)
            {
                ShowError("Select an attachment to upload.");
                return;
            }
            string extension = Path.GetExtension(DocumentFile.FileName).ToLowerInvariant();
            string[] allowed = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
            if (DocumentFile.PostedFile.ContentLength <= 0 || DocumentFile.PostedFile.ContentLength > 20 * 1024 * 1024 ||
                Array.IndexOf(allowed, extension) < 0)
            {
                ShowError("Choose a non-empty PDF, Office document, JPEG, or PNG file no larger than 20 MB.");
                return;
            }
            string storedName = Guid.NewGuid().ToString("N") + extension;
            string uploadRoot = System.Configuration.ConfigurationManager.AppSettings["UploadRoot"] ?? "~/Uploads";
            string folder = Server.MapPath(uploadRoot.TrimEnd('/', '\\') + "/tenders/" + TenderId.ToString(CultureInfo.InvariantCulture));
            string fullPath = Path.Combine(folder, storedName);
            string relativePath = "Uploads/tenders/" + TenderId.ToString(CultureInfo.InvariantCulture) + "/" + storedName;
            try
            {
                Directory.CreateDirectory(folder);
                DocumentFile.SaveAs(fullPath);
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                using (SqlCommand command = new SqlCommand(
                    @"INSERT INTO dbo.TenderDocuments (TenderId,DocumentType,FileName,FilePath,UploadedBy)
                      VALUES (@TenderId,N'Attachment',@FileName,@FilePath,@UploadedBy);
                      SELECT CAST(SCOPE_IDENTITY() AS int);", connection, transaction))
                {
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    AddText(command, "@FileName", SqlDbType.NVarChar, Path.GetFileName(DocumentFile.FileName), 260);
                    AddText(command, "@FilePath", SqlDbType.NVarChar, relativePath, 400);
                    ModuleDb.AddParameter(command, "@UploadedBy", SqlDbType.Int, CurrentUserId);
                    int documentId = Convert.ToInt32(command.ExecuteScalar());
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "Upload", "TenderDocument", documentId, null, Request.UserHostAddress);
                    transaction.Commit();
                }
                Message.Text = "<div class=\"message success\">Attachment uploaded.</div>";
                LoadTender();
            }
            catch (SqlException ex)
            {
                if (File.Exists(fullPath)) File.Delete(fullPath);
                Trace.Warn("DepartmentModule", "Tender document upload record failed.", ex);
                ShowError("The attachment could not be recorded. Please retry.");
            }
            catch (IOException ex)
            {
                Trace.Warn("DepartmentModule", "Tender document file could not be stored.", ex);
                ShowError("The attachment could not be saved.");
            }
        }

        protected void SubmitWorkflowButton_Click(object sender, EventArgs e) { SaveTransition("Submitted"); }
        protected void ApproveButton_Click(object sender, EventArgs e) { SaveTransition("Approved"); }
        protected void RejectButton_Click(object sender, EventArgs e) { SaveTransition("Rejected"); }

        protected void DeleteTenderButton_Click(object sender, EventArgs e)
        {
            if (!HasAnyRole("Admin") || !String.Equals(TenderStatus, "Draft", StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Only an administrator can delete a draft tender.");
                return;
            }

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    using (SqlCommand command = new SqlCommand(
                        @"UPDATE dbo.Tender
                          SET IsDeleted=1,ModifiedBy=@ModifiedBy,ModifiedDate=SYSUTCDATETIME()
                          WHERE TenderId=@TenderId AND CurrentStatus=N'Draft' AND IsDeleted=0;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@ModifiedBy", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        if (command.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("Only an existing draft tender can be deleted.");
                    }

                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "Delete", "Tender",
                        TenderId, "Soft deleted draft tender", Request.UserHostAddress);
                    transaction.Commit();
                }

                Response.Redirect("~/Pages/Tenders.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Tender deletion failed.", ex);
                ShowError("The tender could not be deleted. Please retry or contact support.");
            }
            catch (InvalidOperationException ex)
            {
                ShowError(ex.Message);
            }
        }

        private void SaveTransition(string action)
        {
            WorkflowTransition transition = WorkflowRules.ForStatus(TenderStatus);
            if (transition == null)
            {
                ShowError("No workflow action is available for the current status.");
                return;
            }
            bool isSubmit = action == "Submitted";
            string[] allowedRoles = isSubmit ? transition.SubmitRoles : transition.ApproverRoles;
            if (allowedRoles.Length == 0 || !WorkflowRules.RoleAllowed(allowedRoles, CurrentRole))
            {
                ShowError("Your role is not authorized to perform this workflow action.");
                return;
            }
            if (Remarks.Text.Length > 1000)
            {
                ShowError("Remarks must be no longer than 1,000 characters.");
                return;
            }
            string nextStatus = action == "Rejected" ? transition.RejectStatus : transition.NextStatus;
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    string previousStatus;
                    using (SqlCommand command = new SqlCommand("SELECT CurrentStatus FROM dbo.Tender WITH (UPDLOCK, ROWLOCK) WHERE TenderId=@TenderId AND IsDeleted=0;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        object value = command.ExecuteScalar();
                        if (value == null || value == DBNull.Value) throw new InvalidOperationException("Tender no longer exists.");
                        previousStatus = Convert.ToString(value);
                    }
                    if (!String.Equals(previousStatus, TenderStatus, StringComparison.Ordinal))
                        throw new InvalidOperationException("Tender status changed. Refresh the page before acting.");

                    using (SqlCommand command = new SqlCommand(
                        @"UPDATE dbo.Tender SET CurrentStatus=@NewStatus,ModifiedBy=@UserId,ModifiedDate=SYSUTCDATETIME()
                          WHERE TenderId=@TenderId AND CurrentStatus=@PreviousStatus AND IsDeleted=0;", connection, transaction))
                    {
                        AddText(command, "@NewStatus", SqlDbType.NVarChar, nextStatus, 50);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        AddText(command, "@PreviousStatus", SqlDbType.NVarChar, previousStatus, 50);
                        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Tender status changed. Refresh the page before acting.");
                    }
                    using (SqlCommand command = new SqlCommand(
                        @"INSERT INTO dbo.ApprovalHistory (TenderId,ApprovalStage,PreviousStatus,NewStatus,Status,ApprovedBy,RoleAtAction,Remarks)
                          VALUES (@TenderId,@Stage,@PreviousStatus,@NewStatus,@Action,@UserId,@Role,@Remarks);", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        AddText(command, "@Stage", SqlDbType.NVarChar, transition.Stage, 80);
                        AddText(command, "@PreviousStatus", SqlDbType.NVarChar, previousStatus, 50);
                        AddText(command, "@NewStatus", SqlDbType.NVarChar, nextStatus, 50);
                        AddText(command, "@Action", SqlDbType.NVarChar, action, 20);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                        AddText(command, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
                        AddText(command, "@Remarks", SqlDbType.NVarChar, Remarks.Text, 1000);
                        command.ExecuteNonQuery();
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, action, "Tender", TenderId, nextStatus, Request.UserHostAddress);
                    transaction.Commit();
                }
                Message.Text = "<div class=\"message success\">Tender status changed to " + HttpUtility.HtmlEncode(nextStatus) + ".</div>";
                LoadTender();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Tender workflow transition failed.", ex);
                ShowError("The workflow action could not be saved. Please refresh and retry.");
            }
            catch (InvalidOperationException ex)
            {
                ShowError(ex.Message);
            }
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
