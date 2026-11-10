using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class TenderEvaluation : ModulePage
    {
        protected Literal Message, EvaluationTenderTitle, EvaluationTiming, EvaluationDocumentHistory;
        protected HyperLink PublicationLink, BidDocumentsLink;
        protected Panel OpeningPanel, OpenedPanel, EvaluationDetailPanel, SettingsPanel;
        protected GridView OpeningGrid, EvaluationGrid;
        protected TextBox OpeningDesignation, OpeningRemarks, TechnicalOutcome, FinancialOutcome,
            Observations, Recommendation, DelayDays;
        protected FileUpload EvaluationReportFile;
        protected Button StartEvaluationButton, SaveEvaluationButton, ForwardEvaluationButton, SaveDelayButton;

        private int TenderId { get { return QueryId("tenderId"); } }
        private bool IsAdministrator { get { return HasAnyRole("Admin"); } }
        private bool CanOpen { get { return HasAnyRole("CAFOOfficer", "TenderCommittee", "TenderSection", "Admin"); } }
        private bool CanEvaluate { get { return HasAnyRole("TenderSection", "TenderCommittee", "Admin"); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequireRoles("CAFOOfficer", "TenderCommittee", "TenderSection", "Admin")) return;
            OpeningPanel.Visible = CanOpen;
            OpenedPanel.Visible = CanEvaluate;
            SettingsPanel.Visible = IsAdministrator;
            if (!IsPostBack)
            {
                BindOpeningQueue();
                BindEvaluationQueue();
                if (IsAdministrator) BindEvaluationSettings();
                if (TenderId > 0) BindEvaluationDetails();
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

        private void BindOpeningQueue()
        {
            if (!CanOpen) return;
            string sql = @"SELECT t.TenderId,t.TenderNumber,t.Title,d.DepartmentName,
                                  s.OriginalClosingUtc,s.CurrentClosingUtc
                           FROM dbo.Tender t
                           INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                           INNER JOIN dbo.TenderPublicationSchedule s ON s.TenderId=t.TenderId
                           WHERE t.IsDeleted=0 AND t.CurrentStatus=N'Tender Published'
                             AND s.PublicationStartUtc<=SYSUTCDATETIME()
                             AND s.CurrentClosingUtc<=SYSUTCDATETIME()
                             AND NOT EXISTS(SELECT 1 FROM dbo.TenderBidOpening o WHERE o.TenderId=t.TenderId)" +
                           TenderScope("t") + " ORDER BY s.CurrentClosingUtc;";
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddDepartmentScope(command);
                DataTable data = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(command)) adapter.Fill(data);
                ConvertDatesToIndia(data, "OriginalClosingUtc", "CurrentClosingUtc");
                OpeningGrid.DataSource = data;
                OpeningGrid.DataBind();
            }
        }

        private void BindEvaluationQueue()
        {
            if (!CanEvaluate) return;
            string sql = @"SELECT t.TenderId,t.TenderNumber,t.Title,d.DepartmentName,t.CurrentStatus,
                                  o.ActualOpeningUtc,e.EvaluationEligibleAtUtc,e.EvaluationStatus,e.AssignedTeam
                           FROM dbo.TenderEvaluation e
                           INNER JOIN dbo.Tender t ON t.TenderId=e.TenderId
                           INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                           INNER JOIN dbo.TenderBidOpening o ON o.TenderId=t.TenderId
                           WHERE t.IsDeleted=0" + TenderScope("t") +
                           " ORDER BY e.EvaluationEligibleAtUtc DESC;";
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddDepartmentScope(command);
                DataTable data = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(command)) adapter.Fill(data);
                ConvertDatesToIndia(data, "ActualOpeningUtc", "EvaluationEligibleAtUtc");
                EvaluationGrid.DataSource = data;
                EvaluationGrid.DataBind();
            }
        }

        private void BindEvaluationDetails()
        {
            if (!CanEvaluate) return;
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(
                @"SELECT t.TenderNumber,t.Title,t.CurrentStatus,t.DepartmentId,
                         o.ActualOpeningUtc,o.OriginalClosingUtc,o.CurrentClosingUtc,o.OperatorName,o.Designation,
                         e.EvaluationEligibleAtUtc,e.EvaluationStatus,e.AssignedTeam,e.TechnicalOutcome,
                         e.FinancialOutcome,e.Observations,e.Recommendation,e.ReportFilePath
                  FROM dbo.TenderEvaluation e
                  INNER JOIN dbo.Tender t ON t.TenderId=e.TenderId
                  INNER JOIN dbo.TenderBidOpening o ON o.TenderId=t.TenderId
                  WHERE e.TenderId=@TenderId AND t.IsDeleted=0" + TenderScope("t") + ";", connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                AddDepartmentScope(command);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        EvaluationDetailPanel.Visible = false;
                        ShowError("This tender is not available for evaluation in your department.");
                        return;
                    }

                    string status = Convert.ToString(reader["CurrentStatus"]);
                    string evaluationStatus = Convert.ToString(reader["EvaluationStatus"]);
                    EvaluationDetailPanel.Visible = true;
                    EvaluationTenderTitle.Text = ModuleDb.Html(reader["TenderNumber"]) + " · " +
                        ModuleDb.Html(reader["Title"]) + " · " + ModuleDb.Html(reader["AssignedTeam"]);
                    EvaluationTiming.Text = "Bid opened " +
                        HttpUtility.HtmlEncode(TenderLifecycleFiles.FormatIndiaDateTime(reader["ActualOpeningUtc"])) +
                        " IST. Evaluation eligible from " +
                        HttpUtility.HtmlEncode(TenderLifecycleFiles.FormatIndiaDateTime(reader["EvaluationEligibleAtUtc"])) +
                        " IST. Current status: " + HttpUtility.HtmlEncode(evaluationStatus) + ".";
                    BidDocumentsLink.NavigateUrl = "~/Pages/Bids.aspx?tenderId=" + TenderId.ToString(CultureInfo.InvariantCulture);
                    BidDocumentsLink.Visible = HasAnyRole("TenderSection", "TenderCommittee", "Admin");
                    TechnicalOutcome.Text = Convert.ToString(reader["TechnicalOutcome"]);
                    FinancialOutcome.Text = Convert.ToString(reader["FinancialOutcome"]);
                    Observations.Text = Convert.ToString(reader["Observations"]);
                    Recommendation.Text = Convert.ToString(reader["Recommendation"]);
                    bool awaiting = evaluationStatus == "Awaiting Evaluation";
                    bool underway = evaluationStatus == "Under Evaluation" && status == "Technical Evaluation";
                    StartEvaluationButton.Visible = awaiting && CanEvaluate;
                    DateTime eligibilityUtc = DateTime.SpecifyKind(
                        Convert.ToDateTime(reader["EvaluationEligibleAtUtc"], CultureInfo.InvariantCulture), DateTimeKind.Utc);
                    StartEvaluationButton.Enabled = awaiting && CanEvaluate && DateTime.UtcNow >= eligibilityUtc;
                    if (awaiting && !StartEvaluationButton.Enabled)
                        StartEvaluationButton.ToolTip = "Evaluation is unavailable until the configured eligibility time.";
                    SaveEvaluationButton.Visible = underway && CanEvaluate;
                    ForwardEvaluationButton.Visible = underway && HasAnyRole("TenderSection", "Admin");
                    string reportPath = Convert.ToString(reader["ReportFilePath"]);
                    ForwardEvaluationButton.Enabled = !String.IsNullOrWhiteSpace(reportPath);
                    if (String.IsNullOrWhiteSpace(reportPath))
                        ForwardEvaluationButton.ToolTip = "Save an evaluation report before submitting the recommendation.";
                }
            }

            BindEvaluationDocuments();
        }

        private void BindEvaluationDocuments()
        {
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(
                @"SELECT EvaluationDocumentId,FileName,UploadedAtUtc
                  FROM dbo.TenderEvaluationDocuments
                  WHERE TenderId=@TenderId ORDER BY UploadedAtUtc DESC;", connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    StringBuilder html = new StringBuilder("<h3>Evaluation report history</h3><ul>");
                    bool found = false;
                    while (reader.Read())
                    {
                        found = true;
                        string url = ResolveUrl("~/Pages/DocumentDownload.aspx?evaluationDocumentId=" +
                            Convert.ToString(reader["EvaluationDocumentId"], CultureInfo.InvariantCulture));
                        html.Append("<li><a href=\"").Append(HttpUtility.HtmlAttributeEncode(url)).Append("\">")
                            .Append(HttpUtility.HtmlEncode(reader["FileName"])).Append("</a> · ")
                            .Append(HttpUtility.HtmlEncode(TenderLifecycleFiles.FormatIndiaDateTime(reader["UploadedAtUtc"])))
                            .Append(" IST</li>");
                    }
                    html.Append("</ul>");
                    EvaluationDocumentHistory.Text = found ? html.ToString() : "<p class=\"muted\">No evaluation reports uploaded.</p>";
                }
            }
        }

        private void BindEvaluationSettings()
        {
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(
                "SELECT StartDelayDays FROM dbo.TenderEvaluationSettings WHERE SettingsId=1;", connection))
            {
                object value = command.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                    throw new InvalidOperationException("Tender evaluation settings have not been initialized.");
                DelayDays.Text = Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        protected void OpeningGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "OpenBids") return;
            int tenderId;
            if (!CanOpen || !Int32.TryParse(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture), out tenderId) ||
                tenderId <= 0 || String.IsNullOrWhiteSpace(OpeningDesignation.Text) || OpeningDesignation.Text.Trim().Length > 150 ||
                OpeningRemarks.Text.Length > 1000)
            {
                ShowError("Enter a designation (maximum 150 characters) and valid opening remarks.");
                return;
            }

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand("dbo.sp_OpenTenderBids", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, tenderId);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                    AddText(command, "@RoleAtAction", SqlDbType.NVarChar, CurrentRole, 50);
                    ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                        CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                    AddText(command, "@Designation", SqlDbType.NVarChar, OpeningDesignation.Text, 150);
                    AddText(command, "@Remarks", SqlDbType.NVarChar, OpeningRemarks.Text, 1000);
                    command.ExecuteNonQuery();
                }
                Message.Text = "<div class=\"message success\">Bid opening recorded. The evaluation eligibility time has been scheduled.</div>";
                BindOpeningQueue();
                BindEvaluationQueue();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Tender bids could not be opened.", ex);
                ShowError(ex.Number >= 51000 && ex.Number <= 51099 ? ex.Message : "Tender bids could not be opened.");
            }
        }

        protected void EvaluationGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "SelectEvaluation") return;
            int tenderId;
            if (!CanEvaluate || !Int32.TryParse(Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture), out tenderId) ||
                tenderId <= 0)
            {
                ShowError("The selected evaluation is invalid.");
                return;
            }
            Response.Redirect("~/Pages/TenderEvaluation.aspx?tenderId=" + tenderId.ToString(CultureInfo.InvariantCulture), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void StartEvaluationButton_Click(object sender, EventArgs e)
        {
            if (!CanEvaluate || TenderId <= 0) { ShowError("Your role cannot start evaluation."); return; }
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand("dbo.sp_StartTenderEvaluation", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                    AddText(command, "@RoleAtAction", SqlDbType.NVarChar, CurrentRole, 50);
                    ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                        CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                    command.ExecuteNonQuery();
                }
                Message.Text = "<div class=\"message success\">Evaluation started.</div>";
                BindEvaluationQueue();
                BindEvaluationDetails();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Tender evaluation could not be started.", ex);
                ShowError(ex.Number >= 51000 && ex.Number <= 51099 ? ex.Message : "Tender evaluation could not be started.");
            }
        }

        protected void SaveEvaluationButton_Click(object sender, EventArgs e)
        {
            if (!CanEvaluate || TenderId <= 0 ||
                TechnicalOutcome.Text.Length > 1000 || FinancialOutcome.Text.Length > 1000 ||
                Observations.Text.Length > 4000 || Recommendation.Text.Length > 2000)
            {
                ShowError("Evaluation fields exceed their maximum lengths or your role is not permitted.");
                return;
            }

            string reportPath = null;
            try
            {
                if (EvaluationReportFile.HasFile)
                    reportPath = TenderLifecycleFiles.SaveUpload(this, EvaluationReportFile, TenderId, "evaluation report");

                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    if (reportPath != null)
                    {
                        using (SqlCommand document = new SqlCommand(
                            @"INSERT INTO dbo.TenderEvaluationDocuments(TenderId,FileName,FilePath,UploadedBy)
                              VALUES(@TenderId,@FileName,@FilePath,@UserId);", connection, transaction))
                        {
                            ModuleDb.AddParameter(document, "@TenderId", SqlDbType.Int, TenderId);
                            AddText(document, "@FileName", SqlDbType.NVarChar, TenderLifecycleFiles.SafeFileName(EvaluationReportFile.FileName), 260);
                            AddText(document, "@FilePath", SqlDbType.NVarChar, reportPath, 400);
                            ModuleDb.AddParameter(document, "@UserId", SqlDbType.Int, CurrentUserId);
                            document.ExecuteNonQuery();
                        }
                    }

                    using (SqlCommand command = new SqlCommand(
                        @"UPDATE dbo.TenderEvaluation
                          SET TechnicalOutcome=@TechnicalOutcome,FinancialOutcome=@FinancialOutcome,
                              Observations=@Observations,Recommendation=@Recommendation,
                              ReportFileName=CASE WHEN @ReportFileName IS NULL THEN ReportFileName ELSE @ReportFileName END,
                              ReportFilePath=CASE WHEN @ReportFilePath IS NULL THEN ReportFilePath ELSE @ReportFilePath END,
                              UpdatedBy=@UserId,UpdatedAtUtc=SYSUTCDATETIME()
                          WHERE TenderId=@TenderId AND EvaluationStatus=N'Under Evaluation'
                            AND EXISTS(SELECT 1 FROM dbo.Tender t
                                       WHERE t.TenderId=@TenderId AND t.CurrentStatus=N'Technical Evaluation'
                                         AND EXISTS(SELECT 1 FROM dbo.Users u INNER JOIN dbo.Roles r ON r.RoleId=u.RoleId
                                                    WHERE u.UserId=@UserId AND u.IsActive=1 AND r.RoleName=@Role)" +
                            TenderScope("t") + ");",
                        connection, transaction))
                    {
                        AddText(command, "@TechnicalOutcome", SqlDbType.NVarChar, TechnicalOutcome.Text, 1000);
                        AddText(command, "@FinancialOutcome", SqlDbType.NVarChar, FinancialOutcome.Text, 1000);
                        AddText(command, "@Observations", SqlDbType.NVarChar, Observations.Text, 4000);
                        AddText(command, "@Recommendation", SqlDbType.NVarChar, Recommendation.Text, 2000);
                        AddText(command, "@ReportFileName", SqlDbType.NVarChar,
                            reportPath == null ? null : TenderLifecycleFiles.SafeFileName(EvaluationReportFile.FileName), 260);
                        AddText(command, "@ReportFilePath", SqlDbType.NVarChar, reportPath, 400);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        AddText(command, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
                        AddDepartmentScope(command);
                        if (command.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("Evaluation is not currently open for updates.");
                    }

                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "UpdateEvaluation",
                        "TenderEvaluation", TenderId, Recommendation.Text, Request.UserHostAddress);
                    using (SqlCommand history = new SqlCommand(
                        @"INSERT INTO dbo.TenderLifecycleHistory
                            (TenderId,PreviousStatus,NewStatus,ActionName,ActionBy,RoleAtAction,DepartmentId,Remarks,RelatedDocumentPath)
                          VALUES(@TenderId,N'Technical Evaluation',N'Technical Evaluation',N'Evaluation updated',
                                 @UserId,@Role,@DepartmentId,@Remarks,@DocumentPath);", connection, transaction))
                    {
                        ModuleDb.AddParameter(history, "@TenderId", SqlDbType.Int, TenderId);
                        ModuleDb.AddParameter(history, "@UserId", SqlDbType.Int, CurrentUserId);
                        AddText(history, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
                        ModuleDb.AddParameter(history, "@DepartmentId", SqlDbType.Int,
                            CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                        AddText(history, "@Remarks", SqlDbType.NVarChar, Recommendation.Text, 2000);
                        AddText(history, "@DocumentPath", SqlDbType.NVarChar, reportPath, 400);
                        history.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }

                Message.Text = "<div class=\"message success\">Evaluation saved.</div>";
                BindEvaluationDetails();
            }
            catch (InvalidOperationException ex)
            {
                TenderLifecycleFiles.DeleteStoredUpload(this, reportPath);
                ShowError(ex.Message);
            }
            catch (SqlException ex)
            {
                TenderLifecycleFiles.DeleteStoredUpload(this, reportPath);
                Trace.Warn("DepartmentModule", "Tender evaluation could not be saved.", ex);
                ShowError("Tender evaluation could not be saved.");
            }
            catch (IOException ex)
            {
                Trace.Warn("DepartmentModule", "Evaluation report could not be stored.", ex);
                ShowError("The evaluation report could not be saved.");
            }
        }

        protected void ForwardEvaluationButton_Click(object sender, EventArgs e)
        {
            if (!HasAnyRole("TenderSection", "Admin") || TenderId <= 0)
            {
                ShowError("Only Tender Section or an administrator can submit the evaluation recommendation.");
                return;
            }
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    string previousStatus;
                    string reportPath, technicalOutcome, financialOutcome;
                    using (SqlCommand read = new SqlCommand(
                        @"SELECT t.CurrentStatus,e.ReportFilePath,e.Recommendation,e.TechnicalOutcome,e.FinancialOutcome
                          FROM dbo.Tender t WITH (UPDLOCK,HOLDLOCK)
                          INNER JOIN dbo.TenderEvaluation e ON e.TenderId=t.TenderId
                          WHERE t.TenderId=@TenderId AND t.IsDeleted=0
                            AND EXISTS(SELECT 1 FROM dbo.Users u INNER JOIN dbo.Roles r ON r.RoleId=u.RoleId
                                       WHERE u.UserId=@UserId AND u.IsActive=1 AND r.RoleName=@Role)" +
                          TenderScope("t") + ";",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(read, "@TenderId", SqlDbType.Int, TenderId);
                        ModuleDb.AddParameter(read, "@UserId", SqlDbType.Int, CurrentUserId);
                        AddText(read, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
                        AddDepartmentScope(read);
                        using (SqlDataReader reader = read.ExecuteReader())
                        {
                            if (!reader.Read()) throw new InvalidOperationException("Tender evaluation was not found in your department.");
                            previousStatus = Convert.ToString(reader["CurrentStatus"]);
                            reportPath = Convert.ToString(reader["ReportFilePath"]);
                            technicalOutcome = Convert.ToString(reader["TechnicalOutcome"]);
                            financialOutcome = Convert.ToString(reader["FinancialOutcome"]);
                            if (String.IsNullOrWhiteSpace(Convert.ToString(reader["Recommendation"])))
                                throw new InvalidOperationException("Enter and save an evaluation recommendation before forwarding.");
                        }
                    }
                    if (previousStatus != "Technical Evaluation" || String.IsNullOrWhiteSpace(reportPath))
                        throw new InvalidOperationException("Save the evaluation report and confirm the tender is under evaluation before forwarding.");
                    if (String.IsNullOrWhiteSpace(technicalOutcome) || String.IsNullOrWhiteSpace(financialOutcome))
                        throw new InvalidOperationException("Record both technical and financial evaluation outcomes before forwarding.");

                    using (SqlCommand update = new SqlCommand(
                        @"UPDATE dbo.Tender SET CurrentStatus=N'Technical Evaluation Approved',
                             ModifiedBy=@UserId,ModifiedDate=SYSUTCDATETIME()
                          WHERE TenderId=@TenderId AND CurrentStatus=N'Technical Evaluation';
                          UPDATE dbo.TenderEvaluation
                          SET EvaluationStatus=N'Evaluation Completed',CompletedAtUtc=SYSUTCDATETIME(),
                              UpdatedBy=@UserId,UpdatedAtUtc=SYSUTCDATETIME()
                          WHERE TenderId=@TenderId AND EvaluationStatus=N'Under Evaluation';",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(update, "@UserId", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(update, "@TenderId", SqlDbType.Int, TenderId);
                        if (update.ExecuteNonQuery() != 2)
                            throw new InvalidOperationException("Tender status changed or evaluation is no longer active.");
                    }
                    using (SqlCommand approval = new SqlCommand(
                        @"INSERT INTO dbo.ApprovalHistory
                            (TenderId,ApprovalStage,PreviousStatus,NewStatus,Status,ApprovedBy,RoleAtAction,Remarks)
                          VALUES(@TenderId,N'TechnicalEvaluation',N'Technical Evaluation',
                                 N'Technical Evaluation Approved',N'Approved',@UserId,@Role,@Remarks);", connection, transaction))
                    {
                        ModuleDb.AddParameter(approval, "@TenderId", SqlDbType.Int, TenderId);
                        ModuleDb.AddParameter(approval, "@UserId", SqlDbType.Int, CurrentUserId);
                        AddText(approval, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
                        AddText(approval, "@Remarks", SqlDbType.NVarChar, Recommendation.Text, 1000);
                        approval.ExecuteNonQuery();
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "ForwardEvaluation",
                        "Tender", TenderId, "Technical Evaluation Approved", Request.UserHostAddress);
                    using (SqlCommand history = new SqlCommand(
                        @"INSERT INTO dbo.TenderLifecycleHistory
                            (TenderId,PreviousStatus,NewStatus,ActionName,ActionBy,RoleAtAction,DepartmentId,Remarks,RelatedDocumentPath)
                          VALUES(@TenderId,N'Technical Evaluation',N'Technical Evaluation Approved',
                                 N'Evaluation forwarded',@UserId,@Role,@DepartmentId,@Remarks,@DocumentPath);",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(history, "@TenderId", SqlDbType.Int, TenderId);
                        ModuleDb.AddParameter(history, "@UserId", SqlDbType.Int, CurrentUserId);
                        AddText(history, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
                        ModuleDb.AddParameter(history, "@DepartmentId", SqlDbType.Int,
                            CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                        AddText(history, "@Remarks", SqlDbType.NVarChar, Recommendation.Text, 2000);
                        AddText(history, "@DocumentPath", SqlDbType.NVarChar, reportPath, 400);
                        history.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }

                Message.Text = "<div class=\"message success\">Evaluation forwarded to the existing Tender Committee approval stage.</div>";
                BindEvaluationQueue();
                BindEvaluationDetails();
            }
            catch (InvalidOperationException ex) { ShowError(ex.Message); }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Evaluation could not be forwarded.", ex);
                ShowError("Evaluation could not be forwarded.");
            }
        }

        protected void SaveDelayButton_Click(object sender, EventArgs e)
        {
            int days;
            if (!IsAdministrator || !Int32.TryParse(DelayDays.Text, NumberStyles.None, CultureInfo.InvariantCulture, out days) ||
                days < 0 || days > 365)
            {
                ShowError("Enter an evaluation delay from 0 to 365 days. Only an administrator can change this setting.");
                return;
            }
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    using (SqlCommand command = new SqlCommand(
                        @"IF NOT EXISTS(SELECT 1 FROM dbo.Users u INNER JOIN dbo.Roles r ON r.RoleId=u.RoleId
                                        WHERE u.UserId=@UserId AND u.IsActive=1 AND r.RoleName=N'Admin')
                              THROW 51066, 'Only an administrator can change the evaluation delay.', 1;
                          UPDATE dbo.TenderEvaluationSettings
                          SET StartDelayDays=@Days,ModifiedBy=@UserId,ModifiedAtUtc=SYSUTCDATETIME()
                          WHERE SettingsId=1;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(command, "@Days", SqlDbType.Int, days);
                        if (command.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("Evaluation settings were not initialized by the database migration.");
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "UpdateEvaluationDelay",
                        "TenderEvaluationSettings", 1, days.ToString(CultureInfo.InvariantCulture), Request.UserHostAddress);
                    transaction.Commit();
                }
                Message.Text = "<div class=\"message success\">Evaluation start delay saved. Existing eligibility times are unchanged.</div>";
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Evaluation delay could not be saved.", ex);
                ShowError("Evaluation delay could not be saved.");
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
