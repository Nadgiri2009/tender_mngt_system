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
        protected TextBox TitleText;
        protected TextBox DescriptionText;
        protected DropDownList DepartmentSelect;
        protected DropDownList TenderTypeSelect;
        protected TextBox AmountText;
        protected TextBox DeadlineText;
        protected FileUpload Attachments;
        protected Button CreateButton;

        private static readonly string[] TenderTypes = { "Open", "Limited", "Single", "EOI" };
        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequireRoles("DepartmentOfficer", "Admin")) return;
            if (!IsPostBack)
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

                TenderTypeSelect.Items.Add(new ListItem("Select tender type", ""));
                foreach (string type in TenderTypes) TenderTypeSelect.Items.Add(new ListItem(type, type));
            }
        }

        protected void TitleLength_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = args.Value != null && args.Value.Trim().Length >= 5 && args.Value.Trim().Length <= 250;
        }

        protected void CreateButton_Click(object sender, EventArgs e)
        {
            if (!HasAnyRole("DepartmentOfficer", "Admin")) { RequireRoles("DepartmentOfficer", "Admin"); return; }
            Page.Validate();
            if (!Page.IsValid) return;

            decimal amount;
            int departmentId;
            DateTime deadline;
            DateTime? submissionDeadline = null;
            if (!Decimal.TryParse(AmountText.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) || amount <= 0 ||
                !Int32.TryParse(DepartmentSelect.SelectedValue, out departmentId) || departmentId <= 0 ||
                !Array.Exists(TenderTypes, t => t == TenderTypeSelect.SelectedValue))
            {
                ShowError("Enter a valid department, tender type, and positive estimated amount.");
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
