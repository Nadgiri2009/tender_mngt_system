using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class TenderForm : ModulePage
    {
        protected Literal Message;
        protected Literal FormTitle, FormHeading, FormDescription;
        protected TextBox TitleText;
        protected TextBox DescriptionText;
        protected DropDownList DepartmentSelect;
        protected DropDownList TenderTypeSelect;
        protected TextBox AmountText;
        protected TextBox DeadlineText;
        protected FileUpload Attachments;
        protected Button CreateButton;
        protected Panel DepartmentPanel, AttachmentPanel;
        protected RequiredFieldValidator DepartmentRequired;

        private static readonly string[] TenderTypes = { "Open", "Limited", "Single", "EOI" };
        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
        private int EditingTenderId { get { return QueryId("id"); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            bool isEditing = EditingTenderId > 0;
            if (isEditing)
            {
                if (!RequireRoles("Admin")) return;
            }
            else if (!RequireRoles("DepartmentOfficer", "Admin")) return;

            FormTitle.Text = isEditing ? "Edit Tender" : "New Tender";
            FormHeading.Text = isEditing ? "Edit Tender Requirement" : "New Tender Requirement";
            FormDescription.Text = isEditing
                ? "Only draft tenders can be edited by an administrator."
                : "Create a draft requirement for department processing.";
            CreateButton.Text = isEditing ? "Save Changes" : "Create Tender";
            DepartmentPanel.Visible = !isEditing;
            DepartmentRequired.Enabled = !isEditing;
            AttachmentPanel.Visible = !isEditing;

            if (!IsPostBack)
            {
                try
                {
                    if (!isEditing)
                    {
                        using (SqlConnection connection = ModuleDb.OpenConnection())
                        using (SqlCommand command = new SqlCommand("SELECT DepartmentId, DepartmentName FROM dbo.Departments WHERE IsActive=1 ORDER BY DepartmentName;", connection))
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            DepartmentSelect.Items.Clear();
                            DepartmentSelect.Items.Add(new ListItem("Select department", ""));
                            while (reader.Read())
                            {
                                ListItem item = new ListItem(reader.GetString(1), reader.GetInt32(0).ToString(CultureInfo.InvariantCulture));
                                DepartmentSelect.Items.Add(item);
                                if (CurrentDepartmentId.HasValue && CurrentDepartmentId.Value == reader.GetInt32(0)) item.Selected = true;
                            }
                        }
                    }

                    TenderTypeSelect.Items.Add(new ListItem("Select tender type", ""));
                    foreach (string type in TenderTypes) TenderTypeSelect.Items.Add(new ListItem(type, type));

                    if (isEditing) LoadTenderForEdit();
                }
                catch (SqlException ex)
                {
                    Trace.Warn("DepartmentModule", "Tender form data could not be loaded.", ex);
                    ShowError("Tender form data could not be loaded. Please retry or contact support.");
                    CreateButton.Enabled = false;
                }
            }
        }

        private void LoadTenderForEdit()
        {
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(
                @"SELECT Title,Description,EstimatedAmount,TenderType,SubmissionDeadline,CurrentStatus
                  FROM dbo.Tender WHERE TenderId=@TenderId AND IsDeleted=0;", connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, EditingTenderId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        Response.Redirect("~/Pages/Tenders.aspx", false);
                        Context.ApplicationInstance.CompleteRequest();
                        return;
                    }
                    if (!String.Equals(Convert.ToString(reader["CurrentStatus"]), "Draft", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowError("Only tenders in Draft status can be edited.");
                        CreateButton.Enabled = false;
                        return;
                    }

                    TitleText.Text = Convert.ToString(reader["Title"]);
                    DescriptionText.Text = Convert.ToString(reader["Description"] == DBNull.Value ? String.Empty : reader["Description"]);
                    AmountText.Text = Convert.ToDecimal(reader["EstimatedAmount"]).ToString(CultureInfo.InvariantCulture);
                    string tenderType = Convert.ToString(reader["TenderType"]);
                    if (TenderTypeSelect.Items.FindByValue(tenderType) == null)
                        TenderTypeSelect.Items.Add(new ListItem(tenderType, tenderType));
                    TenderTypeSelect.SelectedValue = tenderType;
                    DeadlineText.Text = reader["SubmissionDeadline"] == DBNull.Value
                        ? String.Empty
                        : Convert.ToDateTime(reader["SubmissionDeadline"]).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
                }
            }
        }

        protected void TitleLength_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = args.Value != null && args.Value.Trim().Length >= 5 && args.Value.Trim().Length <= 250;
        }

        protected void CreateButton_Click(object sender, EventArgs e)
        {
            if (EditingTenderId > 0)
            {
                if (!HasAnyRole("Admin")) { RequireRoles("Admin"); return; }
            }
            else if (!HasAnyRole("DepartmentOfficer", "Admin"))
            {
                RequireRoles("DepartmentOfficer", "Admin");
                return;
            }
            Page.Validate();
            if (!Page.IsValid) return;

            decimal amount;
            int departmentId = 0;
            DateTime deadline;
            DateTime? submissionDeadline = null;
            if (!Decimal.TryParse(AmountText.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) || amount <= 0 ||
                (EditingTenderId == 0 && (!Int32.TryParse(DepartmentSelect.SelectedValue, out departmentId) || departmentId <= 0)) ||
                !Array.Exists(TenderTypes, t => t == TenderTypeSelect.SelectedValue))
            {
                ShowError("Enter a valid department, tender type, and positive estimated amount.");
                return;
            }
            if (DescriptionText.Text != null && DescriptionText.Text.Length > 4000)
            {
                ShowError("Description cannot exceed 4,000 characters.");
                return;
            }
            if (!String.IsNullOrWhiteSpace(DeadlineText.Text))
            {
                if (!DateTime.TryParse(DeadlineText.Text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out deadline))
                {
                    ShowError("Enter a valid submission deadline.");
                    return;
                }
                submissionDeadline = deadline;
            }

            if (EditingTenderId > 0)
            {
                UpdateDraftTender(amount, submissionDeadline);
                return;
            }

            List<PostedFileInfo> files;
            string fileError;
            if (!PrepareUploads(out files, out fileError))
            {
                ShowError(fileError);
                return;
            }

            List<string> savedPaths = new List<string>();
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    int tenderId;
                    int year = DateTime.UtcNow.Year;
                    string tenderNumber;
                    using (SqlCommand command = new SqlCommand(
                        "SELECT COUNT(*) FROM dbo.Departments WHERE DepartmentId=@DepartmentId AND IsActive=1;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int, departmentId);
                        if (Convert.ToInt32(command.ExecuteScalar()) != 1)
                            throw new InvalidOperationException("The selected department is no longer active. Refresh the form and select an active department.");
                    }
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT COUNT(*) FROM dbo.Tender WITH (UPDLOCK, HOLDLOCK)
                          WHERE TenderNumber LIKE @Prefix;", connection, transaction))
                    {
                        AddText(command, "@Prefix", SqlDbType.NVarChar, "TND-" + year.ToString(CultureInfo.InvariantCulture) + "-%", 20);
                        int countThisYear = Convert.ToInt32(command.ExecuteScalar());
                        tenderNumber = "TND-" + year.ToString(CultureInfo.InvariantCulture) + "-" +
                                       (countThisYear + 1).ToString("D5", CultureInfo.InvariantCulture);
                    }

                    using (SqlCommand command = new SqlCommand(
                        @"INSERT INTO dbo.Tender (TenderNumber, TrackingId, Title, Description, DepartmentId, EstimatedAmount, TenderType, CurrentStatus, SubmissionDeadline, CreatedBy)
                          VALUES (@TenderNumber, @TrackingId, @Title, @Description, @DepartmentId, @Amount, @TenderType, N'Draft', @Deadline, @CreatedBy);
                          SELECT CAST(SCOPE_IDENTITY() AS int);", connection, transaction))
                    {
                        AddText(command, "@TenderNumber", SqlDbType.NVarChar, tenderNumber, 50);
                        AddText(command, "@TrackingId", SqlDbType.NVarChar, Guid.NewGuid().ToString(), 100);
                        AddText(command, "@Title", SqlDbType.NVarChar, TitleText.Text.Trim(), 250);
                        AddText(command, "@Description", SqlDbType.NVarChar, DescriptionText.Text, -1);
                        ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int, departmentId);
                        ModuleDb.AddParameter(command, "@Amount", SqlDbType.Decimal, amount);
                        AddText(command, "@TenderType", SqlDbType.NVarChar, TenderTypeSelect.SelectedValue, 50);
                        ModuleDb.AddParameter(command, "@Deadline", SqlDbType.DateTime2, submissionDeadline.HasValue ? (object)submissionDeadline.Value : null);
                        ModuleDb.AddParameter(command, "@CreatedBy", SqlDbType.Int, CurrentUserId);
                        tenderId = Convert.ToInt32(command.ExecuteScalar());
                    }

                    foreach (PostedFileInfo file in files)
                    {
                        string uploadRoot = System.Configuration.ConfigurationManager.AppSettings["UploadRoot"] ?? "~/Uploads";
                        string folder = Server.MapPath(uploadRoot.TrimEnd('/', '\\') + "/tenders/" + tenderId.ToString(CultureInfo.InvariantCulture));
                        Directory.CreateDirectory(folder);
                        string diskName = Guid.NewGuid().ToString("N") + file.Extension;
                        string fullPath = Path.Combine(folder, diskName);
                        file.File.SaveAs(fullPath);
                        string relativeRoot = uploadRoot.Trim('~', '/', '\\').Replace('\\', '/');
                        string relativePath = relativeRoot + "/tenders/" + tenderId.ToString(CultureInfo.InvariantCulture) + "/" + diskName;
                        savedPaths.Add(fullPath);
                        using (SqlCommand command = new SqlCommand(
                            @"INSERT INTO dbo.TenderDocuments (TenderId, DocumentType, FileName, FilePath, UploadedBy)
                              VALUES (@TenderId, N'Attachment', @FileName, @FilePath, @UploadedBy);", connection, transaction))
                        {
                            ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, tenderId);
                            AddText(command, "@FileName", SqlDbType.NVarChar, file.OriginalName, 260);
                            AddText(command, "@FilePath", SqlDbType.NVarChar, relativePath, 400);
                            ModuleDb.AddParameter(command, "@UploadedBy", SqlDbType.Int, CurrentUserId);
                            command.ExecuteNonQuery();
                        }
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "Create", "Tender", tenderId, tenderNumber, Request.UserHostAddress);
                    transaction.Commit();
                    Response.Redirect("~/Pages/TenderDetails.aspx?id=" + tenderId.ToString(CultureInfo.InvariantCulture), false);
                    Context.ApplicationInstance.CompleteRequest();
                }
            }
            catch (SqlException ex)
            {
                DeleteSavedFiles(savedPaths);
                Trace.Warn("DepartmentModule", "Tender creation failed.", ex);
                ShowError("The tender could not be saved. Check that the tender number is unique and retry.");
            }
            catch (IOException ex)
            {
                DeleteSavedFiles(savedPaths);
                Trace.Warn("DepartmentModule", "Tender attachment could not be stored.", ex);
                ShowError("An attachment could not be saved. The tender was not submitted.");
            }
            catch (InvalidOperationException ex)
            {
                DeleteSavedFiles(savedPaths);
                ShowError(ex.Message);
            }
        }

        private void UpdateDraftTender(decimal amount, DateTime? submissionDeadline)
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                using (SqlCommand command = new SqlCommand(
                    @"UPDATE dbo.Tender
                      SET Title=@Title,Description=@Description,EstimatedAmount=@Amount,TenderType=@TenderType,
                          SubmissionDeadline=@Deadline,ModifiedBy=@ModifiedBy,ModifiedDate=SYSUTCDATETIME()
                      WHERE TenderId=@TenderId AND CurrentStatus=N'Draft' AND IsDeleted=0;", connection, transaction))
                {
                    AddText(command, "@Title", SqlDbType.NVarChar, TitleText.Text.Trim(), 250);
                    AddText(command, "@Description", SqlDbType.NVarChar, DescriptionText.Text, -1);
                    ModuleDb.AddParameter(command, "@Amount", SqlDbType.Decimal, amount);
                    AddText(command, "@TenderType", SqlDbType.NVarChar, TenderTypeSelect.SelectedValue, 50);
                    ModuleDb.AddParameter(command, "@Deadline", SqlDbType.DateTime2,
                        submissionDeadline.HasValue ? (object)submissionDeadline.Value : null);
                    ModuleDb.AddParameter(command, "@ModifiedBy", SqlDbType.Int, CurrentUserId);
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, EditingTenderId);
                    if (command.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("Only an existing draft tender can be edited.");

                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "Update", "Tender",
                        EditingTenderId, null, Request.UserHostAddress);
                    transaction.Commit();
                }

                Response.Redirect("~/Pages/TenderDetails.aspx?id=" + EditingTenderId.ToString(CultureInfo.InvariantCulture), false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Tender update failed.", ex);
                ShowError("The tender could not be updated. Please retry or contact support.");
            }
            catch (InvalidOperationException ex)
            {
                ShowError(ex.Message);
            }
        }

        private bool PrepareUploads(out List<PostedFileInfo> files, out string error)
        {
            files = new List<PostedFileInfo>();
            error = null;
            if (!Attachments.HasFiles) return true;
            foreach (HttpPostedFile file in Attachments.PostedFiles)
            {
                string originalName = Path.GetFileName(file.FileName);
                string extension = Path.GetExtension(originalName);
                if (file.ContentLength <= 0 || file.ContentLength > 20 * 1024 * 1024)
                {
                    error = "Each attachment must be non-empty and no larger than 20 MB.";
                    return false;
                }
                if (Array.IndexOf(AllowedExtensions, extension.ToLowerInvariant()) < 0)
                {
                    error = "An attachment has a file type that is not permitted.";
                    return false;
                }
                files.Add(new PostedFileInfo { File = file, OriginalName = originalName, Extension = extension.ToLowerInvariant() });
            }
            return true;
        }

        private static void DeleteSavedFiles(List<string> paths)
        {
            foreach (string path in paths) if (File.Exists(path)) File.Delete(path);
        }

        private void ShowError(string text)
        {
            Message.Text = "<div class=\"message\">" + HttpUtility.HtmlEncode(text) + "</div>";
        }

        private sealed class PostedFileInfo
        {
            public HttpPostedFile File { get; set; }
            public string OriginalName { get; set; }
            public string Extension { get; set; }
        }
    }
}
