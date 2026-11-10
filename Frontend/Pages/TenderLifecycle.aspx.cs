using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class TenderLifecycle : ModulePage
    {
        protected Literal Message, SelectedTender, PublicationPeriod;
        protected HyperLink EvaluationLink;
        protected Panel PublishedTendersPanel, SchedulePanel, ExtensionRequestPanel,
            PendingRequestsPanel, ExtensionHistoryPanel;
        protected GridView PublishedTendersGrid, PendingRequestsGrid, ExtensionHistoryGrid;
        protected TextBox StartDate, StartTime, ClosingDate, ClosingTime, ScheduleRemarks,
            ProposedClosingDate, ProposedClosingTime, ExtensionReason, ReviewRemarks;
        protected FileUpload ApplicationFile, SupportingFile;
        protected Button SaveScheduleButton, SubmitExtensionButton;

        private int TenderId { get { return QueryId("tenderId"); } }
        private bool IsAdministrator { get { return HasAnyRole("Admin"); } }
        private bool CanReview { get { return HasAnyRole("CAFOOfficer", "Admin"); } }
        private bool CanRequest { get { return HasAnyRole("DepartmentOfficer", "Admin"); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequireRoles("DepartmentOfficer", "CAFOOfficer", "Admin")) return;
            EvaluationLink.Visible = HasAnyRole("CAFOOfficer", "TenderCommittee", "TenderSection", "Admin");
            SchedulePanel.Visible = CanReview && TenderId > 0;
            ExtensionRequestPanel.Visible = CanRequest && TenderId > 0;
            PendingRequestsPanel.Visible = CanReview;
            if (!IsPostBack)
            {
                BindTenders();
                BindPendingRequests();
                BindHistory();
                if (TenderId > 0) BindSelectedTender();
            }
        }

        private string TenderScope(string alias)
        {
            if (IsAdministrator) return String.Empty;
            return " AND (@DepartmentId IS NULL OR " + alias + @".DepartmentId=@DepartmentId)
                    AND (NOT EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                    WHERE scopeUser.UserId=@ScopeUserId AND scopeUser.DepartmentId IS NOT NULL)
                         OR EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                   WHERE scopeUser.UserId=@ScopeUserId AND scopeUser.DepartmentId=" + alias + ".DepartmentId))";
        }

        private void AddDepartmentScope(SqlCommand command)
        {
            if (!IsAdministrator)
            {
                ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                    CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                ModuleDb.AddParameter(command, "@ScopeUserId", SqlDbType.Int, CurrentUserId);
            }
        }

        private void BindTenders()
        {
            string ownerScope = HasAnyRole("DepartmentOfficer") && !IsAdministrator
                ? " AND t.CreatedBy=@UserId" : String.Empty;
            string sql = @"SELECT t.TenderId,t.TenderNumber,t.Title,d.DepartmentName,
                                  s.PublicationStartUtc,s.OriginalClosingUtc,s.CurrentClosingUtc,
                                  (SELECT COUNT(*) FROM dbo.TenderExtensionRequests e
                                   WHERE e.TenderId=t.TenderId AND e.ReviewStatus=N'Approved') AS ApprovedExtensions,
                                  (SELECT COUNT(*) FROM dbo.TenderExtensionRequests e
                                   WHERE e.TenderId=t.TenderId AND e.ReviewStatus=N'Pending') AS PendingExtensions
                           FROM dbo.Tender t
                           INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                           LEFT JOIN dbo.TenderPublicationSchedule s ON s.TenderId=t.TenderId
                           WHERE t.IsDeleted=0 AND t.CurrentStatus=N'Tender Published'" + TenderScope("t") + ownerScope +
                           " ORDER BY s.CurrentClosingUtc,t.TenderNumber;";
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddDepartmentScope(command);
                if (ownerScope.Length > 0)
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                DataTable data = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(command)) adapter.Fill(data);
                ConvertDatesToIndia(data, "PublicationStartUtc", "OriginalClosingUtc", "CurrentClosingUtc");
                PublishedTendersGrid.DataSource = data;
                PublishedTendersGrid.DataBind();
            }
        }

        private void BindSelectedTender()
        {
            string ownership = HasAnyRole("DepartmentOfficer") && !IsAdministrator
                ? " AND t.CreatedBy=@UserId" : String.Empty;
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(
                @"SELECT t.TenderNumber,t.Title,t.CurrentStatus,d.DepartmentName,
                         s.PublicationStartUtc,s.OriginalClosingUtc,s.CurrentClosingUtc,s.PublicationPeriodDays,
                         s.Remarks,s.ApprovedAtUtc
                  FROM dbo.Tender t
                  INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                  LEFT JOIN dbo.TenderPublicationSchedule s ON s.TenderId=t.TenderId
                  WHERE t.TenderId=@TenderId AND t.IsDeleted=0 AND t.CurrentStatus=N'Tender Published'" +
                  TenderScope("t") + ownership + ";", connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                AddDepartmentScope(command);
                if (ownership.Length > 0) ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        SchedulePanel.Visible = false;
                        ExtensionRequestPanel.Visible = false;
                        ShowError("This tender is not available for lifecycle actions in your role or department.");
                        return;
                    }

                    SelectedTender.Text = ModuleDb.Html(reader["TenderNumber"]) + " · " +
                        ModuleDb.Html(reader["Title"]) + " · " + ModuleDb.Html(reader["DepartmentName"]);
                    if (reader["CurrentClosingUtc"] != DBNull.Value)
                    {
                        PublicationPeriod.Text = "<p class=\"message success\">Current approved period: " +
                            HttpUtility.HtmlEncode(TenderLifecycleFiles.FormatIndiaDateTime(reader["PublicationStartUtc"])) +
                            " to " + HttpUtility.HtmlEncode(TenderLifecycleFiles.FormatIndiaDateTime(reader["CurrentClosingUtc"])) +
                            " IST (" + HttpUtility.HtmlEncode(Convert.ToString(reader["PublicationPeriodDays"], CultureInfo.InvariantCulture)) +
                            " days). Approved " + HttpUtility.HtmlEncode(TenderLifecycleFiles.FormatIndiaDateTime(reader["ApprovedAtUtc"])) +
                            " IST.</p>";
                        if (reader["Remarks"] != DBNull.Value)
                            PublicationPeriod.Text += "<p>" + HttpUtility.HtmlEncode(Convert.ToString(reader["Remarks"])) + "</p>";
                    }
                    else
                    {
                        PublicationPeriod.Text = "<p class=\"message\">No publication schedule has been approved.</p>";
                    }
                }
            }
        }

        private void BindPendingRequests()
        {
            if (!CanReview) return;
            string sql = @"SELECT e.ExtensionRequestId,e.ExtensionNumber,e.OriginalClosingUtc,e.ProposedClosingUtc,
                                  e.ApplicationFileName,e.SupportingFileName,e.Reason,u.FullName AS SubmittedByName,
                                  t.TenderNumber,t.Title,d.DepartmentName
                           FROM dbo.TenderExtensionRequests e
                           INNER JOIN dbo.Tender t ON t.TenderId=e.TenderId
                           INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                           INNER JOIN dbo.Users u ON u.UserId=e.SubmittedBy
                           WHERE e.ReviewStatus=N'Pending' AND t.IsDeleted=0" + TenderScope("t") +
                           " ORDER BY e.SubmittedAtUtc;";
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddDepartmentScope(command);
                DataTable data = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(command)) adapter.Fill(data);
                ConvertDatesToIndia(data, "OriginalClosingUtc", "ProposedClosingUtc");
                PendingRequestsGrid.DataSource = data;
                PendingRequestsGrid.DataBind();
            }
        }

        private void BindHistory()
        {
            string ownerScope = HasAnyRole("DepartmentOfficer") && !IsAdministrator
                ? " AND t.CreatedBy=@UserId" : String.Empty;
            string sql = @"SELECT e.ExtensionRequestId,e.ExtensionNumber,e.OriginalClosingUtc,e.ProposedClosingUtc,e.Reason,
                                  e.ApplicationFileName,e.SupportingFileName,
                                  e.SubmittedAtUtc,e.ReviewStatus,e.ReviewedAtUtc,e.ReviewRemarks,
                                  su.FullName AS SubmittedByName,ru.FullName AS ReviewedByName,t.TenderNumber
                           FROM dbo.TenderExtensionRequests e
                           INNER JOIN dbo.Tender t ON t.TenderId=e.TenderId
                           INNER JOIN dbo.Users su ON su.UserId=e.SubmittedBy
                           LEFT JOIN dbo.Users ru ON ru.UserId=e.ReviewedBy
                           WHERE t.IsDeleted=0" + TenderScope("t") + ownerScope +
                           (TenderId > 0 ? " AND t.TenderId=@TenderId" : String.Empty) +
                           " ORDER BY e.SubmittedAtUtc DESC;";
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddDepartmentScope(command);
                if (ownerScope.Length > 0)
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                if (TenderId > 0) ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                DataTable data = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(command)) adapter.Fill(data);
                ConvertDatesToIndia(data, "OriginalClosingUtc", "ProposedClosingUtc", "SubmittedAtUtc", "ReviewedAtUtc");
                ExtensionHistoryGrid.DataSource = data;
                ExtensionHistoryGrid.DataBind();
            }
        }

        protected void SaveScheduleButton_Click(object sender, EventArgs e)
        {
            if (!CanReview || TenderId <= 0) { ShowError("Your role cannot approve publication schedules."); return; }
            if (ScheduleRemarks.Text.Length > 1000) { ShowError("Remarks must not exceed 1,000 characters."); return; }
            try
            {
                DateTime startUtc = TenderLifecycleFiles.ParseIndiaDateTime(StartDate.Text, StartTime.Text, "publication start");
                DateTime closingUtc = TenderLifecycleFiles.ParseIndiaDateTime(ClosingDate.Text, ClosingTime.Text, "publication closing");
                if (closingUtc <= startUtc) throw new InvalidOperationException("The closing time must be after the publication start.");

                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand("dbo.sp_SaveTenderPublicationSchedule", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    ModuleDb.AddParameter(command, "@PublicationStartUtc", SqlDbType.DateTime2, startUtc);
                    ModuleDb.AddParameter(command, "@ClosingUtc", SqlDbType.DateTime2, closingUtc);
                    AddText(command, "@Remarks", SqlDbType.NVarChar, ScheduleRemarks.Text, 1000);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                    AddText(command, "@RoleAtAction", SqlDbType.NVarChar, CurrentRole, 50);
                    ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                        CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                    command.ExecuteNonQuery();
                }

                Message.Text = "<div class=\"message success\">Publication schedule approved and recorded.</div>";
                BindSelectedTender();
                BindTenders();
            }
            catch (InvalidOperationException ex) { ShowError(ex.Message); }
            catch (ArgumentException ex) { ShowError("The server's India time zone configuration is unavailable: " + ex.Message); }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Publication schedule approval failed.", ex);
                ShowError(ex.Number >= 51000 && ex.Number <= 51099 ? ex.Message : "The publication schedule could not be saved.");
            }
        }

        protected void SubmitExtensionButton_Click(object sender, EventArgs e)
        {
            if (!CanRequest || TenderId <= 0) { ShowError("Your role cannot request a tender extension."); return; }
            if (ExtensionReason.Text.Trim().Length == 0 || ExtensionReason.Text.Length > 1000)
            {
                ShowError("Enter an extension reason (maximum 1,000 characters).");
                return;
            }

            string applicationPath = null, supportingPath = null;
            try
            {
                DateTime proposedUtc = TenderLifecycleFiles.ParseIndiaDateTime(
                    ProposedClosingDate.Text, ProposedClosingTime.Text, "proposed closing");
                EnsureExtensionRequestAllowed(proposedUtc);
                applicationPath = TenderLifecycleFiles.SaveUpload(this, ApplicationFile, TenderId, "extension application");
                supportingPath = TenderLifecycleFiles.SaveUpload(this, SupportingFile, TenderId, "supporting");

                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand("dbo.sp_CreateTenderExtensionRequest", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    ModuleDb.AddParameter(command, "@ProposedClosingUtc", SqlDbType.DateTime2, proposedUtc);
                    AddText(command, "@ApplicationFileName", SqlDbType.NVarChar, TenderLifecycleFiles.SafeFileName(ApplicationFile.FileName), 260);
                    AddText(command, "@ApplicationFilePath", SqlDbType.NVarChar, applicationPath, 400);
                    AddText(command, "@SupportingFileName", SqlDbType.NVarChar, TenderLifecycleFiles.SafeFileName(SupportingFile.FileName), 260);
                    AddText(command, "@SupportingFilePath", SqlDbType.NVarChar, supportingPath, 400);
                    AddText(command, "@Reason", SqlDbType.NVarChar, ExtensionReason.Text, 1000);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                    AddText(command, "@RoleAtAction", SqlDbType.NVarChar, CurrentRole, 50);
                    ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                        CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                    command.ExecuteScalar();
                }

                Message.Text = "<div class=\"message success\">Extension request submitted for CAFO review.</div>";
                BindTenders();
                BindHistory();
            }
            catch (InvalidOperationException ex)
            {
                TenderLifecycleFiles.DeleteStoredUpload(this, applicationPath);
                TenderLifecycleFiles.DeleteStoredUpload(this, supportingPath);
                ShowError(ex.Message);
            }
            catch (SqlException ex)
            {
                TenderLifecycleFiles.DeleteStoredUpload(this, applicationPath);
                TenderLifecycleFiles.DeleteStoredUpload(this, supportingPath);
                Trace.Warn("DepartmentModule", "Tender extension request failed.", ex);
                ShowError(ex.Number >= 51000 && ex.Number <= 51099 ? ex.Message : "The extension request could not be submitted.");
            }
            catch (System.IO.IOException ex)
            {
                TenderLifecycleFiles.DeleteStoredUpload(this, applicationPath);
                TenderLifecycleFiles.DeleteStoredUpload(this, supportingPath);
                Trace.Warn("DepartmentModule", "Extension documents could not be stored.", ex);
                ShowError("The extension documents could not be saved.");
            }
        }

        private void EnsureExtensionRequestAllowed(DateTime proposedClosingUtc)
        {
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(
                @"SELECT t.CurrentStatus,t.CreatedBy,t.DepartmentId,s.CurrentClosingUtc,
                         (SELECT COUNT(*) FROM dbo.TenderExtensionRequests e
                          WHERE e.TenderId=t.TenderId AND e.ReviewStatus=N'Approved') AS ApprovedExtensions,
                         (SELECT COUNT(*) FROM dbo.TenderExtensionRequests e
                          WHERE e.TenderId=t.TenderId AND e.ReviewStatus=N'Pending') AS PendingExtensions,
                         u.DepartmentId AS UserDepartmentId,
                         CASE WHEN EXISTS(SELECT 1 FROM dbo.Users activeUser
                                          INNER JOIN dbo.Roles r ON r.RoleId=activeUser.RoleId
                                          WHERE activeUser.UserId=@UserId AND activeUser.IsActive=1
                                            AND r.RoleName=@Role) THEN 1 ELSE 0 END AS IsAuthorized
                  FROM dbo.Tender t
                  LEFT JOIN dbo.TenderPublicationSchedule s ON s.TenderId=t.TenderId
                  INNER JOIN dbo.Users u ON u.UserId=@UserId
                  WHERE t.TenderId=@TenderId AND t.IsDeleted=0;", connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                AddText(command, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read() || Convert.ToInt32(reader["IsAuthorized"], CultureInfo.InvariantCulture) != 1)
                        throw new InvalidOperationException("The tender or your active role could not be verified.");
                    if (Convert.ToString(reader["CurrentStatus"]) != "Tender Published")
                        throw new InvalidOperationException("Extensions are only available for published tenders.");
                    if (!IsAdministrator && Convert.ToInt32(reader["CreatedBy"], CultureInfo.InvariantCulture) != CurrentUserId)
                        throw new InvalidOperationException("You may only request extensions for tenders you own.");

                    int tenderDepartment = Convert.ToInt32(reader["DepartmentId"], CultureInfo.InvariantCulture);
                    if (!IsAdministrator &&
                        ((CurrentDepartmentId.HasValue && CurrentDepartmentId.Value != tenderDepartment) ||
                         (reader["UserDepartmentId"] != DBNull.Value &&
                          Convert.ToInt32(reader["UserDepartmentId"], CultureInfo.InvariantCulture) != tenderDepartment)))
                        throw new InvalidOperationException("The tender is outside your department.");

                    if (Convert.ToInt32(reader["ApprovedExtensions"], CultureInfo.InvariantCulture) >= 3)
                        throw new InvalidOperationException("The maximum of three approved extensions has been reached.");
                    if (Convert.ToInt32(reader["PendingExtensions"], CultureInfo.InvariantCulture) > 0)
                        throw new InvalidOperationException("An extension request is already awaiting review.");
                    if (reader["CurrentClosingUtc"] == DBNull.Value)
                        throw new InvalidOperationException("The tender has no approved publication schedule.");

                    DateTime currentClosingUtc = DateTime.SpecifyKind(
                        Convert.ToDateTime(reader["CurrentClosingUtc"], CultureInfo.InvariantCulture), DateTimeKind.Utc);
                    if (currentClosingUtc <= DateTime.UtcNow)
                        throw new InvalidOperationException("An extension must be requested before publication closes.");
                    if (proposedClosingUtc <= currentClosingUtc)
                        throw new InvalidOperationException("The proposed closing time must be after the current closing time.");
                }
            }
        }

        protected void PendingRequestsGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (!CanReview || (e.CommandName != "ApproveExtension" && e.CommandName != "RejectExtension"))
            {
                ShowError("Your role cannot review this extension request.");
                return;
            }
            int requestId;
            if (!Int32.TryParse(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture), out requestId) || requestId <= 0)
            {
                ShowError("The selected extension request is invalid.");
                return;
            }

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand("dbo.sp_ReviewTenderExtensionRequest", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    ModuleDb.AddParameter(command, "@ExtensionRequestId", SqlDbType.Int, requestId);
                    AddText(command, "@Decision", SqlDbType.NVarChar,
                        e.CommandName == "ApproveExtension" ? "Approve" : "Reject", 10);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                    AddText(command, "@RoleAtAction", SqlDbType.NVarChar, CurrentRole, 50);
                    ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                        CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                    AddText(command, "@Remarks", SqlDbType.NVarChar, ReviewRemarks.Text, 1000);
                    command.ExecuteNonQuery();
                }

                Message.Text = "<div class=\"message success\">Extension request " +
                    (e.CommandName == "ApproveExtension" ? "approved; closing time updated." : "rejected; closing time unchanged.") +
                    "</div>";
                BindTenders();
                BindPendingRequests();
                BindHistory();
                if (TenderId > 0) BindSelectedTender();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Tender extension review failed.", ex);
                ShowError(ex.Number >= 51000 && ex.Number <= 51099 ? ex.Message : "The extension decision could not be saved.");
            }
        }

        private static void ConvertDatesToIndia(DataTable data, params string[] columns)
        {
            foreach (string column in columns)
            {
                if (!data.Columns.Contains(column)) continue;
                foreach (DataRow row in data.Rows)
                {
                    if (row[column] != DBNull.Value)
                    {
                        row[column] = TimeZoneInfo.ConvertTimeFromUtc(
                            DateTime.SpecifyKind(Convert.ToDateTime(row[column], CultureInfo.InvariantCulture), DateTimeKind.Utc),
                            TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                    }
                }
            }
        }

        private void ShowError(string text)
        {
            Message.Text = "<div class=\"message\">" + HttpUtility.HtmlEncode(text) + "</div>";
        }
    }
}
