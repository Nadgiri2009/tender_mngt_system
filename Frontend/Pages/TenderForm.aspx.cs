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
        protected TextBox BudgetInformationText, TechnicalSanctionNumberText, TechnicalSanctionAuthorityText,
            TechnicalSanctionDateText, PreBidMeetingText, PreBidLocationText, PreBidDetailsText,
            TechnicalBidOpeningText;
        protected DropDownList DepartmentSelect;
        protected DropDownList TenderTypeSelect;
        protected DropDownList TenderCategorySelect;
        protected DropDownList DocumentTypeSelect;
        protected TextBox AmountText;
        protected TextBox StartText, DeadlineText;
        protected FileUpload Attachments;
        protected Button CreateButton;
        protected Panel DepartmentPanel, AttachmentPanel;
        protected RequiredFieldValidator DepartmentRequired;

        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
        private int EditingTenderId { get { return QueryId("id"); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            bool isEditing = EditingTenderId > 0;
            bool isJe = HasAnyRole("Junior Engineer (JE)");
            if (isEditing)
            {
                if (!RequireRoles("Admin", "DepartmentOfficer")) return;
            }
            else if (!RequireAnyPermission("tender.create.department", "tenders.manage.all")) return;

            FormTitle.Text = isEditing ? "Edit Tender" : "New Tender";
            FormHeading.Text = isEditing ? "Edit Tender Requirement" : "New Tender Requirement";
            FormDescription.Text = isEditing
                ? "Drafts are editable by administrators. Returned budget proposals can be revised by their owning department officer."
                : "Create a draft requirement for department processing.";
            CreateButton.Text = isEditing ? "Save Changes" : "Create Tender";
            DepartmentPanel.Visible = !isEditing && !isJe;
            DepartmentRequired.Enabled = !isEditing;
            AttachmentPanel.Visible = !isEditing;

            if (!IsPostBack)
            {
                try
                {
                    using (SqlConnection connection = ModuleDb.OpenConnection())
                    {
                        if (!isEditing && !isJe)
                        {
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

                        BindTenderCatalog(connection, TenderTypeSelect, "SELECT MethodName FROM dbo.TenderMethods WHERE IsActive=1 ORDER BY SortOrder,MethodName;", "Select tender method");
                        BindTenderCatalog(connection, TenderCategorySelect, "SELECT TypeName FROM dbo.TenderTypes WHERE IsActive=1 ORDER BY SortOrder,TypeName;", "Select tender category");
                        BindTenderCatalog(connection, DocumentTypeSelect, "SELECT TypeName FROM dbo.DocumentTypes WHERE IsActive=1 ORDER BY SortOrder,TypeName;", "Select attachment type");
                    }

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

        private static void BindTenderCatalog(SqlConnection connection, DropDownList control, string query, string placeholder)
        {
            control.Items.Clear();
            control.Items.Add(new ListItem(placeholder, ""));
            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string value = Convert.ToString(reader[0]);
                    control.Items.Add(new ListItem(value, value));
                }
            }
        }

        private void LoadTenderForEdit()
        {
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(
                @"SELECT Title,Description,EstimatedAmount,TenderType,TenderCategory,SubmissionStartDate,SubmissionDeadline,CurrentStatus
                  FROM dbo.Tender t
                  WHERE TenderId=@TenderId AND IsDeleted=0 AND WorkflowVersion=1
                    AND (@Role=N'Admin'
                         OR (@Role=N'DepartmentOfficer' AND t.CreatedBy=@UserId
                             AND t.CurrentStatus=N'Budget Revision Required'
                             AND (@DepartmentId IS NULL OR t.DepartmentId=@DepartmentId)
                             AND (NOT EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                             WHERE scopeUser.UserId=@UserId AND scopeUser.DepartmentId IS NOT NULL)
                                  OR EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                            WHERE scopeUser.UserId=@UserId AND scopeUser.DepartmentId=t.DepartmentId))));", connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, EditingTenderId);
                AddText(command, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
                ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                    CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        Response.Redirect("~/Pages/Tenders.aspx", false);
                        Context.ApplicationInstance.CompleteRequest();
                        return;
                    }
                    string status = Convert.ToString(reader["CurrentStatus"]);
                    if (!String.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase) &&
                        !String.Equals(status, "Budget Revision Required", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowError("This tender is not an editable draft or returned budget proposal.");
                        CreateButton.Enabled = false;
                        return;
                    }
                    FormDescription.Text = String.Equals(status, "Budget Revision Required", StringComparison.OrdinalIgnoreCase)
                        ? "Revise the returned estimate and proposal. Submit the updated proposal from tender details when ready."
                        : "Only administrators can edit a tender that is still in draft.";

                    TitleText.Text = Convert.ToString(reader["Title"]);
                    DescriptionText.Text = Convert.ToString(reader["Description"] == DBNull.Value ? String.Empty : reader["Description"]);
                    AmountText.Text = Convert.ToDecimal(reader["EstimatedAmount"]).ToString(CultureInfo.InvariantCulture);
                    string tenderType = Convert.ToString(reader["TenderType"]);
                    if (TenderTypeSelect.Items.FindByValue(tenderType) == null)
                        TenderTypeSelect.Items.Add(new ListItem(tenderType, tenderType));
                    TenderTypeSelect.SelectedValue = tenderType;
                    string tenderCategory = Convert.ToString(reader["TenderCategory"] == DBNull.Value ? String.Empty : reader["TenderCategory"]);
                    if (tenderCategory.Length > 0 && TenderCategorySelect.Items.FindByValue(tenderCategory) == null)
                        TenderCategorySelect.Items.Add(new ListItem(tenderCategory, tenderCategory));
                    if (tenderCategory.Length > 0) TenderCategorySelect.SelectedValue = tenderCategory;
                    StartText.Text = reader["SubmissionStartDate"] == DBNull.Value
                        ? String.Empty
                        : Convert.ToDateTime(reader["SubmissionStartDate"]).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
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
                if (!HasAnyRole("Admin", "DepartmentOfficer")) { RequireRoles("Admin", "DepartmentOfficer"); return; }
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
            bool validTenderCatalog;
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    validTenderCatalog =
                        IsActiveCatalogValue(connection, "dbo.TenderMethods", "MethodName", TenderTypeSelect.SelectedValue) &&
                        IsActiveCatalogValue(connection, "dbo.TenderTypes", "TypeName", TenderCategorySelect.SelectedValue);
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Tender reference data could not be validated.", ex);
                ShowError("Tender method or category could not be validated. Please retry or contact support.");
                return;
            }
            DateTime start;
            DateTime deadline;
            DateTime? submissionStart = null;
            DateTime? submissionDeadline = null;
            if (!Decimal.TryParse(AmountText.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) || amount <= 0 ||
                (EditingTenderId == 0 && !TryGetAuthorizedDepartmentId(out departmentId)) ||
                !validTenderCatalog)
            {
                ShowError("Select an active department, tender method, tender category, and positive estimated amount.");
                return;
            }
            if (DescriptionText.Text != null && DescriptionText.Text.Length > 4000)
            {
                ShowError("Description cannot exceed 4,000 characters.");
                return;
            }
            if ((BudgetInformationText.Text != null && BudgetInformationText.Text.Length > 2000) ||
                (TechnicalSanctionNumberText.Text != null && TechnicalSanctionNumberText.Text.Length > 100) ||
                (TechnicalSanctionAuthorityText.Text != null && TechnicalSanctionAuthorityText.Text.Length > 200) ||
                (PreBidLocationText.Text != null && PreBidLocationText.Text.Length > 300) ||
                (PreBidDetailsText.Text != null && PreBidDetailsText.Text.Length > 2000))
            {
                ShowError("Budget, sanction, and pre-bid details exceed the allowed field lengths.");
                return;
            }
            DateTime sanctionDate = DateTime.MinValue;
            DateTime preBidMeetingUtc = DateTime.MinValue;
            DateTime technicalBidOpeningUtc = DateTime.MinValue;
            bool hasSanctionDate = !String.IsNullOrWhiteSpace(TechnicalSanctionDateText.Text);
            bool hasPreBidMeeting = !String.IsNullOrWhiteSpace(PreBidMeetingText.Text);
            bool hasTechnicalBidOpening = !String.IsNullOrWhiteSpace(TechnicalBidOpeningText.Text);
            if ((hasSanctionDate && !TryParseDate(TechnicalSanctionDateText.Text, out sanctionDate)) ||
                (hasPreBidMeeting && !TryParseIndiaLocalDateTime(PreBidMeetingText.Text, out preBidMeetingUtc)) ||
                (hasTechnicalBidOpening && !TryParseIndiaLocalDateTime(TechnicalBidOpeningText.Text, out technicalBidOpeningUtc)))
            {
                ShowError("Enter valid technical-sanction, pre-bid, and technical-opening dates.");
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
            if (!String.IsNullOrWhiteSpace(StartText.Text))
            {
                if (!DateTime.TryParse(StartText.Text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out start))
                {
                    ShowError("Enter a valid submission start date.");
                    return;
                }
                submissionStart = start;
            }
            if (submissionStart.HasValue &&
                (!submissionDeadline.HasValue || submissionStart.Value > submissionDeadline.Value))
            {
                ShowError("The submission start date must be on or before the submission deadline.");
                return;
            }

            if (EditingTenderId > 0)
            {
                UpdateDraftTender(amount, submissionStart, submissionDeadline);
                return;
            }

            List<PostedFileInfo> files;
            string fileError;
            if (!PrepareUploads(out files, out fileError))
            {
                ShowError(fileError);
                return;
            }
            try
            {
                if (files.Count > 0 && !TenderLifecycleFiles.IsAllowedTenderDocumentType(DocumentTypeSelect.SelectedValue))
                {
                    ShowError("Select an active attachment type.");
                    return;
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Document type could not be validated.", ex);
                ShowError("Attachment type could not be validated. Please retry or contact support.");
                return;
            }

            List<string> savedPaths = new List<string>();
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    if (!IsActiveCatalogValue(connection, "dbo.TenderMethods", "MethodName", TenderTypeSelect.SelectedValue, transaction) ||
                        !IsActiveCatalogValue(connection, "dbo.TenderTypes", "TypeName", TenderCategorySelect.SelectedValue, transaction))
                        throw new InvalidOperationException("The selected tender method or category is no longer active. Refresh the form and retry.");
                    if (files.Count > 0 &&
                        !IsActiveCatalogValue(connection, "dbo.DocumentTypes", "TypeName", DocumentTypeSelect.SelectedValue, transaction))
                        throw new InvalidOperationException("The selected attachment type is no longer active. Refresh the form and retry.");

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
                        @"INSERT INTO dbo.Tender
                            (TenderNumber,TrackingId,Title,Description,DepartmentId,EstimatedAmount,TenderType,TenderCategory,
                             CurrentStatus,SubmissionStartDate,SubmissionDeadline,CreatedBy,WorkflowVersion,
                             BudgetInformation,TechnicalSanctionNumber,TechnicalSanctionAuthority,TechnicalSanctionDate,
                             PreBidMeetingUtc,PreBidLocation,PreBidDetails,TechnicalBidOpeningScheduledUtc)
                          VALUES
                            (@TenderNumber,@TrackingId,@Title,@Description,@DepartmentId,@Amount,@TenderType,@TenderCategory,
                             N'Draft',@StartDate,@Deadline,@CreatedBy,2,@BudgetInformation,@SanctionNumber,
                             @SanctionAuthority,@SanctionDate,@PreBidMeetingUtc,@PreBidLocation,@PreBidDetails,
                             @TechnicalBidOpeningUtc);
                          SELECT CAST(SCOPE_IDENTITY() AS int);", connection, transaction))
                    {
                        AddText(command, "@TenderNumber", SqlDbType.NVarChar, tenderNumber, 50);
                        AddText(command, "@TrackingId", SqlDbType.NVarChar, Guid.NewGuid().ToString(), 100);
                        AddText(command, "@Title", SqlDbType.NVarChar, TitleText.Text.Trim(), 250);
                        AddText(command, "@Description", SqlDbType.NVarChar, DescriptionText.Text, -1);
                        ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int, departmentId);
                        ModuleDb.AddParameter(command, "@Amount", SqlDbType.Decimal, amount);
                        AddText(command, "@TenderType", SqlDbType.NVarChar, TenderTypeSelect.SelectedValue, 50);
                        AddText(command, "@TenderCategory", SqlDbType.NVarChar, TenderCategorySelect.SelectedValue, 50);
                        ModuleDb.AddParameter(command, "@StartDate", SqlDbType.DateTime2,
                            submissionStart.HasValue ? (object)submissionStart.Value : null);
                        ModuleDb.AddParameter(command, "@Deadline", SqlDbType.DateTime2, submissionDeadline.HasValue ? (object)submissionDeadline.Value : null);
                        ModuleDb.AddParameter(command, "@CreatedBy", SqlDbType.Int, CurrentUserId);
                        AddText(command, "@BudgetInformation", SqlDbType.NVarChar, BudgetInformationText.Text, 2000);
                        AddText(command, "@SanctionNumber", SqlDbType.NVarChar, TechnicalSanctionNumberText.Text, 100);
                        AddText(command, "@SanctionAuthority", SqlDbType.NVarChar, TechnicalSanctionAuthorityText.Text, 200);
                        ModuleDb.AddParameter(command, "@SanctionDate", SqlDbType.Date,
                            hasSanctionDate ? (object)sanctionDate.Date : null);
                        ModuleDb.AddParameter(command, "@PreBidMeetingUtc", SqlDbType.DateTime2,
                            hasPreBidMeeting ? (object)preBidMeetingUtc : null);
                        AddText(command, "@PreBidLocation", SqlDbType.NVarChar, PreBidLocationText.Text, 300);
                        AddText(command, "@PreBidDetails", SqlDbType.NVarChar, PreBidDetailsText.Text, 2000);
                        ModuleDb.AddParameter(command, "@TechnicalBidOpeningUtc", SqlDbType.DateTime2,
                            hasTechnicalBidOpening ? (object)technicalBidOpeningUtc : null);
                        tenderId = Convert.ToInt32(command.ExecuteScalar());
                    }

                    int jeRoleId;
                    int initialStageId;
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT r.RoleId,s.StageId
                          FROM dbo.Roles r
                          CROSS JOIN dbo.WorkflowStages s
                          WHERE r.RoleName=N'Junior Engineer (JE)' AND r.IsActive=1
                            AND s.WorkflowVersion=2 AND s.StageName=N'Final_JE_SubmitTender';", connection, transaction))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new InvalidOperationException("The initial finalized workflow stage is not configured.");
                        jeRoleId = Convert.ToInt32(reader["RoleId"]);
                        initialStageId = Convert.ToInt32(reader["StageId"]);
                    }
                    using (SqlCommand command = new SqlCommand(
                        @"INSERT INTO dbo.TenderWorkflowAssignments
                            (TenderId,StageId,AssignedRoleId,AssignedUserId,DepartmentId,IsCurrent)
                          VALUES(@TenderId,@StageId,@RoleId,@AssignedUserId,@DepartmentId,1);",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, tenderId);
                        ModuleDb.AddParameter(command, "@StageId", SqlDbType.Int, initialStageId);
                        ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, jeRoleId);
                        ModuleDb.AddParameter(command, "@AssignedUserId", SqlDbType.Int,
                            HasAnyRole("Junior Engineer (JE)") ? (object)CurrentUserId : null);
                        ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int, departmentId);
                        command.ExecuteNonQuery();
                    }
                    using (SqlCommand command = new SqlCommand(
                        @"INSERT INTO dbo.TenderWorkflowHistory
                            (TenderId,WorkflowVersion,StageId,PreviousStatus,NewStatus,ActionName,
                             UserId,RoleId,DepartmentId,Remarks)
                          VALUES(@TenderId,2,@StageId,NULL,N'Draft',N'Created',@UserId,@RoleId,@DepartmentId,@Remarks);",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, tenderId);
                        ModuleDb.AddParameter(command, "@StageId", SqlDbType.Int, initialStageId);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, CurrentRoleId);
                        ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int, departmentId);
                        AddText(command, "@Remarks", SqlDbType.NVarChar, "Draft tender created.", 2000);
                        command.ExecuteNonQuery();
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
                              VALUES (@TenderId, @DocumentType, @FileName, @FilePath, @UploadedBy);", connection, transaction))
                        {
                            ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, tenderId);
                            AddText(command, "@DocumentType", SqlDbType.NVarChar, DocumentTypeSelect.SelectedValue, 160);
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

        private bool TryGetAuthorizedDepartmentId(out int departmentId)
        {
            departmentId = 0;
            if (HasAnyRole("Junior Engineer (JE)"))
            {
                if (!CurrentDepartmentId.HasValue) return false;
                departmentId = CurrentDepartmentId.Value;
                return true;
            }
            return Int32.TryParse(DepartmentSelect.SelectedValue, out departmentId) && departmentId > 0;
        }

        private static bool TryParseDate(string value, out DateTime result)
        {
            return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out result);
        }

        private static bool TryParseIndiaLocalDateTime(string value, out DateTime utc)
        {
            utc = DateTime.MinValue;
            DateTime localTime;
            if (!DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out localTime))
                return false;
            TimeZoneInfo indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            utc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified), indiaTimeZone);
            return true;
        }

        private static bool IsActiveCatalogValue(SqlConnection connection, string tableName, string columnName,
            string value, SqlTransaction transaction = null)
        {
            string query = "SELECT COUNT(*) FROM " + tableName + " WHERE " + columnName + "=@Value AND IsActive=1;";
            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                AddText(command, "@Value", SqlDbType.NVarChar, value, 80);
                return Convert.ToInt32(command.ExecuteScalar()) == 1;
            }
        }

        private void UpdateDraftTender(decimal amount, DateTime? submissionStart, DateTime? submissionDeadline)
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    if (!IsActiveCatalogValue(connection, "dbo.TenderMethods", "MethodName", TenderTypeSelect.SelectedValue, transaction) ||
                        !IsActiveCatalogValue(connection, "dbo.TenderTypes", "TypeName", TenderCategorySelect.SelectedValue, transaction))
                        throw new InvalidOperationException("The selected tender method or category is no longer active. Refresh the form and retry.");
                    string currentStatus;
                    using (SqlCommand read = new SqlCommand(
                        @"SELECT t.CurrentStatus FROM dbo.Tender t WITH (UPDLOCK,ROWLOCK)
                          WHERE t.TenderId=@TenderId AND t.IsDeleted=0 AND t.WorkflowVersion=1
                            AND (@Role=N'Admin' OR
                                 (@Role=N'DepartmentOfficer' AND t.CreatedBy=@UserId
                                  AND t.CurrentStatus=N'Budget Revision Required'
                                  AND (@DepartmentId IS NULL OR t.DepartmentId=@DepartmentId)
                                  AND (NOT EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                                  WHERE scopeUser.UserId=@UserId AND scopeUser.DepartmentId IS NOT NULL)
                                       OR EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                                 WHERE scopeUser.UserId=@UserId AND scopeUser.DepartmentId=t.DepartmentId))));",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(read, "@TenderId", SqlDbType.Int, EditingTenderId);
                        AddText(read, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
                        ModuleDb.AddParameter(read, "@UserId", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(read, "@DepartmentId", SqlDbType.Int,
                            CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                        object value = read.ExecuteScalar();
                        if (value == null || value == DBNull.Value)
                            throw new InvalidOperationException("This tender cannot be edited in your role or department.");
                        currentStatus = Convert.ToString(value);
                    }
                    if (currentStatus != "Draft" && currentStatus != "Budget Revision Required")
                        throw new InvalidOperationException("Only a draft or returned budget proposal can be edited.");

                    using (SqlCommand command = new SqlCommand(
                        @"UPDATE dbo.Tender
                          SET Title=@Title,Description=@Description,EstimatedAmount=@Amount,TenderType=@TenderType,
                              TenderCategory=@TenderCategory,
                              SubmissionStartDate=@StartDate,SubmissionDeadline=@Deadline,
                              ModifiedBy=@ModifiedBy,ModifiedDate=SYSUTCDATETIME()
                          WHERE TenderId=@TenderId AND CurrentStatus=@ExpectedStatus AND IsDeleted=0 AND WorkflowVersion=1
                            AND (@Role=N'Admin' OR (@Role=N'DepartmentOfficer' AND CreatedBy=@UserId));", connection, transaction))
                    {
                        AddText(command, "@Title", SqlDbType.NVarChar, TitleText.Text.Trim(), 250);
                        AddText(command, "@Description", SqlDbType.NVarChar, DescriptionText.Text, -1);
                        ModuleDb.AddParameter(command, "@Amount", SqlDbType.Decimal, amount);
                        AddText(command, "@TenderType", SqlDbType.NVarChar, TenderTypeSelect.SelectedValue, 50);
                        AddText(command, "@TenderCategory", SqlDbType.NVarChar, TenderCategorySelect.SelectedValue, 50);
                        ModuleDb.AddParameter(command, "@StartDate", SqlDbType.DateTime2,
                            submissionStart.HasValue ? (object)submissionStart.Value : null);
                        ModuleDb.AddParameter(command, "@Deadline", SqlDbType.DateTime2,
                            submissionDeadline.HasValue ? (object)submissionDeadline.Value : null);
                        ModuleDb.AddParameter(command, "@ModifiedBy", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, EditingTenderId);
                        AddText(command, "@ExpectedStatus", SqlDbType.NVarChar, currentStatus, 50);
                        AddText(command, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                        if (command.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("Tender status changed; refresh the page before editing.");
                    }

                    if (currentStatus == "Budget Revision Required")
                    {
                        using (SqlCommand history = new SqlCommand(
                            @"INSERT INTO dbo.TenderLifecycleHistory
                                (TenderId,PreviousStatus,NewStatus,ActionName,ActionBy,RoleAtAction,DepartmentId,Remarks)
                              VALUES(@TenderId,@Status,@Status,N'Budget proposal revised',@UserId,@Role,@DepartmentId,@Remarks);",
                            connection, transaction))
                        {
                            ModuleDb.AddParameter(history, "@TenderId", SqlDbType.Int, EditingTenderId);
                            AddText(history, "@Status", SqlDbType.NVarChar, currentStatus, 50);
                            ModuleDb.AddParameter(history, "@UserId", SqlDbType.Int, CurrentUserId);
                            AddText(history, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
                            ModuleDb.AddParameter(history, "@DepartmentId", SqlDbType.Int,
                                CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                            AddText(history, "@Remarks", SqlDbType.NVarChar, DescriptionText.Text, 1000);
                            history.ExecuteNonQuery();
                        }
                    }

                    ModuleDb.AddAudit(connection, transaction, CurrentUserId,
                        currentStatus == "Budget Revision Required" ? "ReviseBudgetProposal" : "Update",
                        "Tender", EditingTenderId, null, Request.UserHostAddress);
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
