using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class TenderWorkflow : ModulePage
    {
        protected Literal Message, TenderNumber, TenderTitle, DepartmentName, StageName, CurrentStatus, EstimatedAmount;
        protected Panel QueuePanel, DetailPanel, SubmitDraftPanel, PublishPanel, PrimaryOpeningPanel,
            PrimaryVerificationPanel, PrimaryForwardPanel;
        protected GridView WorkflowQueue, HistoryGrid, PrimaryBidderGrid, PrimaryDocumentGrid;
        protected TextBox SubmitRemarksText, PublicationStartText, PublicationCloseText, PublishRemarksText,
            PrimaryOpenRemarksText, PrimaryCompletionRemarksText, PrimaryForwardRemarksText,
            RequiredBidDocumentsText;
        protected Button SubmitDraftButton, PublishButton, OpenTechnicalBidsButton,
            CompletePrimaryVerificationButton, ForwardToTenderCellButton;

        private int TenderId { get { return QueryId("id"); } }
        private bool IsSystemAdministrator { get { return HasPermission("system.admin"); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequireAnyPermission(
                "tender.create.department", "tender.publication.manage", "tender.primary.open",
                "tender.primary.verify", "tender.cell.verify", "shortfall.create", "shortfall.approve",
                "shortfall.respond.own", "shortfall.clear", "financial.open.approve",
                "financial.open.execute", "financial.approve.final", "workorder.manage",
                "agreement.manage", "tenders.monitor", "system.admin")) return;

            if (!IsPostBack)
            {
                if (TenderId > 0) LoadTender();
                else LoadQueue();
            }
        }

        private void LoadQueue()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                    @"SELECT t.TenderId,t.TenderNumber,t.Title,d.DepartmentName,s.StageName,t.CurrentStatus,a.AssignedAtUtc
                      FROM dbo.TenderWorkflowAssignments a
                      INNER JOIN dbo.Tender t ON t.TenderId=a.TenderId AND t.WorkflowVersion=2 AND t.IsDeleted=0
                      INNER JOIN dbo.WorkflowStages s ON s.StageId=a.StageId AND s.WorkflowVersion=2
                      INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                      WHERE a.IsCurrent=1
                        AND (@IsAdmin=1 OR
                             (a.AssignedRoleId=@RoleId AND
                              ((@IsContractor=1 AND a.AssignedUserId=@UserId)
                               OR (@IsContractor=0 AND (a.AssignedUserId IS NULL OR a.AssignedUserId=@UserId)
                                   AND (@IsDepartmentScopedRole=0 OR
                                        (@DepartmentId IS NOT NULL AND a.DepartmentId=@DepartmentId))))))
                      ORDER BY a.AssignedAtUtc,t.TenderId;", connection))
                {
                    ModuleDb.AddParameter(command, "@IsAdmin", SqlDbType.Bit, IsSystemAdministrator);
                    ModuleDb.AddParameter(command, "@IsContractor", SqlDbType.Bit, HasAnyRole("Contractor"));
                    ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, CurrentRoleId);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                    ModuleDb.AddParameter(command, "@IsDepartmentScopedRole", SqlDbType.Bit,
                        HasAnyRole("Junior Engineer (JE)"));
                    ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                        CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataTable data = new DataTable();
                        adapter.Fill(data);
                        WorkflowQueue.DataSource = data;
                        WorkflowQueue.DataBind();
                    }
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Finalized workflow queue could not be loaded.", ex);
                ShowMessage("The workflow queue could not be loaded. Please retry or contact support.");
            }
        }

        private void LoadTender()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT t.TenderNumber,t.Title,t.CurrentStatus,t.EstimatedAmount,d.DepartmentName,
                                 s.StageName,s.StageOrder,a.AssignedRoleId,a.AssignedUserId,a.DepartmentId
                          FROM dbo.TenderWorkflowAssignments a
                          INNER JOIN dbo.Tender t ON t.TenderId=a.TenderId AND t.WorkflowVersion=2 AND t.IsDeleted=0
                          INNER JOIN dbo.WorkflowStages s ON s.StageId=a.StageId AND s.WorkflowVersion=2
                          INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                          WHERE a.TenderId=@TenderId AND a.IsCurrent=1
                            AND (@IsAdmin=1 OR
                                 (a.AssignedRoleId=@RoleId AND
                                  ((@IsContractor=1 AND a.AssignedUserId=@UserId)
                                   OR (@IsContractor=0 AND (a.AssignedUserId IS NULL OR a.AssignedUserId=@UserId)
                                       AND (@IsDepartmentScopedRole=0 OR
                                            (@DepartmentId IS NOT NULL AND a.DepartmentId=@DepartmentId))))));", connection))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        ModuleDb.AddParameter(command, "@IsAdmin", SqlDbType.Bit, IsSystemAdministrator);
                        ModuleDb.AddParameter(command, "@IsContractor", SqlDbType.Bit, HasAnyRole("Contractor"));
                        ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, CurrentRoleId);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(command, "@IsDepartmentScopedRole", SqlDbType.Bit,
                            HasAnyRole("Junior Engineer (JE)"));
                        ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                            CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                DenyAccess();
                                return;
                            }
                            TenderNumber.Text = ModuleDb.Html(reader["TenderNumber"]);
                            TenderTitle.Text = ModuleDb.Html(reader["Title"]);
                            DepartmentName.Text = ModuleDb.Html(reader["DepartmentName"]);
                            CurrentStatus.Text = ModuleDb.Html(reader["CurrentStatus"]);
                            EstimatedAmount.Text = Convert.ToDecimal(reader["EstimatedAmount"])
                                .ToString("N2", CultureInfo.CurrentCulture);
                            StageName.Text = ModuleDb.Html(reader["StageName"]);
                            string stageName = Convert.ToString(reader["StageName"]);
                            bool assignedToCurrentRole = Convert.ToInt32(reader["AssignedRoleId"]) == CurrentRoleId;
                            bool assignedToCurrentUser = reader["AssignedUserId"] == DBNull.Value ||
                                Convert.ToInt32(reader["AssignedUserId"]) == CurrentUserId;
                            bool assignedToCurrentDepartment = CurrentDepartmentId.HasValue &&
                                Convert.ToInt32(reader["DepartmentId"]) == CurrentDepartmentId.Value;
                            bool canAct = assignedToCurrentRole && assignedToCurrentUser &&
                                (IsSystemAdministrator || HasAnyRole("Contractor") ||
                                 !HasAnyRole("Junior Engineer (JE)") || assignedToCurrentDepartment);
                            SubmitDraftPanel.Visible = canAct && stageName == "Final_JE_SubmitTender" &&
                                HasPermission("tender.create.department");
                            PublishPanel.Visible = canAct && stageName == "Final_JE_PublishTender" &&
                                HasPermission("tender.publication.manage");
                            PrimaryOpeningPanel.Visible = canAct && stageName == "Final_JE_OpenTechnicalBids" &&
                                HasPermission("tender.primary.open");
                            PrimaryVerificationPanel.Visible = canAct &&
                                stageName == "Final_JE_CompletePrimaryVerification" &&
                                HasPermission("tender.primary.verify");
                            PrimaryForwardPanel.Visible = canAct && stageName == "Final_JE_ForwardToTenderCell" &&
                                HasPermission("tender.primary.verify");
                        }
                    }
                    if (SubmitDraftPanel.Visible)
                        LoadRequiredBidDocumentTypes(connection);
                    if (PrimaryVerificationPanel.Visible)
                        BindPrimaryVerification(connection);
                    BindHistory(connection);
                }
                QueuePanel.Visible = false;
                DetailPanel.Visible = true;
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Finalized workflow details could not be loaded.", ex);
                ShowMessage("Workflow details could not be loaded. Please retry or contact support.");
            }
        }

        private void BindHistory(SqlConnection connection)
        {
            using (SqlCommand command = new SqlCommand(
                @"SELECT h.ActionAtUtc,s.StageName,h.PreviousStatus,h.NewStatus,h.ActionName,
                         u.FullName AS Actor,h.Remarks
                  FROM dbo.TenderWorkflowHistory h
                  LEFT JOIN dbo.WorkflowStages s ON s.StageId=h.StageId
                  INNER JOIN dbo.Users u ON u.UserId=h.UserId
                  WHERE h.TenderId=@TenderId ORDER BY h.WorkflowHistoryId DESC;", connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                DataTable data = new DataTable();
                adapter.Fill(data);
                HistoryGrid.DataSource = data;
                HistoryGrid.DataBind();
            }
        }

        private void LoadRequiredBidDocumentTypes(SqlConnection connection)
        {
            List<string> documentTypes = new List<string>();
            using (SqlCommand command = new SqlCommand(
                @"SELECT DocumentType FROM dbo.TenderRequiredBidDocuments
                  WHERE TenderId=@TenderId ORDER BY DocumentType;", connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read()) documentTypes.Add(Convert.ToString(reader["DocumentType"]));
                }
            }
            RequiredBidDocumentsText.Text = String.Join(Environment.NewLine, documentTypes.ToArray());
        }

        protected void SubmitDraftButton_Click(object sender, EventArgs e)
        {
            ProcessStage("Final_JE_SubmitTender", "tender.create.department", "Submitted",
                SubmitRemarksText.Text, null, null);
        }

        protected void PublishButton_Click(object sender, EventArgs e)
        {
            DateTime publicationStartUtc, publicationCloseUtc;
            if (!TryParseIndiaDateTime(PublicationStartText.Text, out publicationStartUtc) ||
                !TryParseIndiaDateTime(PublicationCloseText.Text, out publicationCloseUtc) ||
                publicationStartUtc <= DateTime.UtcNow || publicationCloseUtc <= publicationStartUtc)
            {
                ShowMessage("Enter a future publication start and a closing date and time after the start.");
                return;
            }
            ProcessStage("Final_JE_PublishTender", "tender.publication.manage", "Tender Published",
                PublishRemarksText.Text, publicationStartUtc, publicationCloseUtc);
        }

        protected void OpenTechnicalBidsButton_Click(object sender, EventArgs e)
        {
            ProcessStage("Final_JE_OpenTechnicalBids", "tender.primary.open",
                "Primary Verification Pending", PrimaryOpenRemarksText.Text, null, null);
        }

        protected void CompletePrimaryVerificationButton_Click(object sender, EventArgs e)
        {
            ProcessStage("Final_JE_CompletePrimaryVerification", "tender.primary.verify",
                "Primary Verification Completed", PrimaryCompletionRemarksText.Text, null, null);
        }

        protected void ForwardToTenderCellButton_Click(object sender, EventArgs e)
        {
            ProcessStage("Final_JE_ForwardToTenderCell", "tender.primary.verify",
                "Tender Cell Verification Pending", PrimaryForwardRemarksText.Text, null, null);
        }

        private void BindPrimaryVerification(SqlConnection connection)
        {
            using (SqlCommand command = new SqlCommand(
                @"SELECT b.TechnicalBidId,b.ContractorId,c.Name AS ContractorName,c.RegistrationNo,
                         c.ContactNo,c.Email,b.SubmittedDate,
                         (SELECT COUNT(1) FROM dbo.TechnicalBidDocuments d
                          WHERE d.TechnicalBidId=b.TechnicalBidId) AS DocumentCount,
                         (SELECT COUNT(1) FROM dbo.TenderRequiredBidDocuments r
                          WHERE r.TenderId=b.TenderId) AS RequiredDocumentCount,
                         COALESCE(review.Decision,N'') AS PrimaryDecision,
                         COALESCE(review.Remarks,N'') AS PrimaryRemarks
                  FROM dbo.TechnicalBid b
                  INNER JOIN dbo.Contractors c ON c.ContractorId=b.ContractorId
                  OUTER APPLY
                  (
                      SELECT TOP (1) r.Decision,r.Remarks
                      FROM dbo.TenderVerificationReviews r
                      WHERE r.TenderId=b.TenderId AND r.ContractorId=b.ContractorId
                        AND r.ReviewStage=N'Primary'
                      ORDER BY r.ReviewedAtUtc DESC,r.VerificationReviewId DESC
                  ) review
                  WHERE b.TenderId=@TenderId AND b.Status=N'Submitted'
                  ORDER BY b.SubmittedDate,b.TechnicalBidId;", connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                DataTable data = new DataTable();
                adapter.Fill(data);
                PrimaryBidderGrid.DataSource = data;
                PrimaryBidderGrid.DataBind();
            }

            using (SqlCommand command = new SqlCommand(
                @"SELECT d.TechnicalBidDocumentId,b.ContractorId,d.DocumentType,d.FileName,
                         d.UploadedDate,COALESCE(history.VerificationStatus,N'') AS VerificationStatus,
                         COALESCE(history.Remarks,N'') AS VerificationRemarks,CONVERT(bit,0) AS IsMissing,
                         c.Name AS ContractorName
                  FROM dbo.TechnicalBidDocuments d
                  INNER JOIN dbo.TechnicalBid b ON b.TechnicalBidId=d.TechnicalBidId
                  INNER JOIN dbo.Contractors c ON c.ContractorId=b.ContractorId
                  OUTER APPLY
                  (
                      SELECT TOP (1) h.VerificationStatus,h.Remarks
                      FROM dbo.TenderDocumentVerificationHistory h
                      WHERE h.TenderId=b.TenderId AND h.ContractorId=b.ContractorId
                        AND h.TechnicalBidDocumentId=d.TechnicalBidDocumentId
                        AND h.ReviewStage=N'Primary'
                      ORDER BY h.ReviewedAtUtc DESC,h.DocumentVerificationHistoryId DESC
                  ) history
                  WHERE b.TenderId=@TenderId AND b.Status=N'Submitted'
                  UNION ALL
                  SELECT CONVERT(int,0),b.ContractorId,r.DocumentType,N'(not submitted)',
                         CONVERT(datetime2,NULL),COALESCE(history.VerificationStatus,N''),
                         COALESCE(history.Remarks,N''),CONVERT(bit,1),c.Name
                  FROM dbo.TechnicalBid b
                  INNER JOIN dbo.Contractors c ON c.ContractorId=b.ContractorId
                  INNER JOIN dbo.TenderRequiredBidDocuments r ON r.TenderId=b.TenderId
                  OUTER APPLY
                  (
                      SELECT TOP (1) h.VerificationStatus,h.Remarks
                      FROM dbo.TenderDocumentVerificationHistory h
                      WHERE h.TenderId=b.TenderId AND h.ContractorId=b.ContractorId
                        AND h.TechnicalBidDocumentId IS NULL AND h.DocumentType=r.DocumentType
                        AND h.ReviewStage=N'Primary'
                      ORDER BY h.ReviewedAtUtc DESC,h.DocumentVerificationHistoryId DESC
                  ) history
                  WHERE b.TenderId=@TenderId AND b.Status=N'Submitted'
                    AND NOT EXISTS
                    (
                        SELECT 1 FROM dbo.TechnicalBidDocuments d
                        WHERE d.TechnicalBidId=b.TechnicalBidId AND d.DocumentType=r.DocumentType
                    )
                  ORDER BY ContractorName,DocumentType,UploadedDate,TechnicalBidDocumentId;", connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                DataTable data = new DataTable();
                adapter.Fill(data);
                PrimaryDocumentGrid.DataSource = data;
                PrimaryDocumentGrid.DataBind();
            }
        }

        protected bool SupportsInlinePreview(object fileName)
        {
            string extension = Path.GetExtension(Convert.ToString(fileName));
            return String.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase);
        }

        protected void PrimaryBidderGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "RecordPrimaryBidderReview") return;
            int rowIndex;
            if (!Int32.TryParse(Convert.ToString(e.CommandArgument), out rowIndex) ||
                rowIndex < 0 || rowIndex >= PrimaryBidderGrid.DataKeys.Count)
            {
                ShowMessage("The selected bidder could not be verified. Refresh the page and try again.");
                return;
            }

            int contractorId = Convert.ToInt32(PrimaryBidderGrid.DataKeys[rowIndex].Values["ContractorId"]);
            int technicalBidId = Convert.ToInt32(PrimaryBidderGrid.DataKeys[rowIndex].Values["TechnicalBidId"]);
            TextBox remarksInput = PrimaryBidderGrid.Rows[rowIndex].FindControl("PrimaryBidderRemarksText") as TextBox;
            string remarks = remarksInput == null ? String.Empty : remarksInput.Text.Trim();
            if (remarks.Length > 2000)
            {
                ShowMessage("Bidder review remarks cannot exceed 2,000 characters.");
                return;
            }

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    EnsureAssignedStage(connection, transaction, "Final_JE_CompletePrimaryVerification",
                        "tender.primary.verify");
                    int documentCount;
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT COUNT(1) FROM dbo.TechnicalBidDocuments d
                          INNER JOIN dbo.TechnicalBid b ON b.TechnicalBidId=d.TechnicalBidId
                          WHERE b.TenderId=@TenderId AND b.ContractorId=@ContractorId
                            AND b.Status=N'Submitted';", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int, contractorId);
                        documentCount = Convert.ToInt32(command.ExecuteScalar());
                    }
                    if (documentCount == 0 && String.IsNullOrWhiteSpace(remarks))
                        throw new InvalidOperationException("Record remarks explaining the missing bidder documents.");

                    using (SqlCommand command = new SqlCommand(
                        @"IF EXISTS
                          (
                              SELECT 1 FROM dbo.TenderVerificationReviews WITH (UPDLOCK,HOLDLOCK)
                              WHERE TenderId=@TenderId AND ContractorId=@ContractorId AND ReviewStage=N'Primary'
                          )
                              THROW 51110,'A primary bidder review has already been recorded.',1;
                          IF NOT EXISTS
                          (
                              SELECT 1 FROM dbo.TechnicalBid
                              WHERE TenderId=@TenderId AND ContractorId=@ContractorId AND Status=N'Submitted'
                          )
                              THROW 51111,'The submitted bidder record is no longer available.',1;
                          INSERT INTO dbo.TenderVerificationReviews
                            (TenderId,ContractorId,ReviewStage,Decision,ReviewerId,ReviewerRoleId,Remarks)
                          VALUES(@TenderId,@ContractorId,N'Primary',N'Completed',@UserId,@RoleId,@Remarks);",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int, contractorId);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, CurrentRoleId);
                        AddText(command, "@Remarks", SqlDbType.NVarChar, remarks, 2000);
                        command.ExecuteNonQuery();
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "PrimaryBidderReview",
                        "TechnicalBid", technicalBidId, remarks, Request.UserHostAddress);
                    transaction.Commit();
                }

                Message.Text = "<div class=\"message success\">Bidder review recorded.</div>";
                BindPrimaryVerificationAfterPostback();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Primary bidder review could not be recorded.", ex);
                ShowMessage(ex.Number == 51110
                    ? "A primary bidder review has already been recorded."
                    : "The bidder review could not be saved.");
            }
            catch (InvalidOperationException ex) { ShowMessage(ModuleDb.Html(ex.Message)); }
        }

        protected void PrimaryDocumentGrid_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "RecordPrimaryDocument") return;
            int rowIndex;
            if (!Int32.TryParse(Convert.ToString(e.CommandArgument), out rowIndex) ||
                rowIndex < 0 || rowIndex >= PrimaryDocumentGrid.DataKeys.Count)
            {
                ShowMessage("The selected document could not be verified. Refresh the page and try again.");
                return;
            }

            int documentId = Convert.ToInt32(PrimaryDocumentGrid.DataKeys[rowIndex].Value);
            int contractorId = Convert.ToInt32(PrimaryDocumentGrid.DataKeys[rowIndex].Values["ContractorId"]);
            string documentType = Convert.ToString(PrimaryDocumentGrid.DataKeys[rowIndex].Values["DocumentType"]);
            bool isMissing = Convert.ToBoolean(PrimaryDocumentGrid.DataKeys[rowIndex].Values["IsMissing"]);
            DropDownList decisionInput = PrimaryDocumentGrid.Rows[rowIndex].FindControl("PrimaryDocumentDecision") as DropDownList;
            TextBox remarksInput = PrimaryDocumentGrid.Rows[rowIndex].FindControl("PrimaryDocumentRemarksText") as TextBox;
            string decision = decisionInput == null ? String.Empty : decisionInput.SelectedValue;
            string remarks = remarksInput == null ? String.Empty : remarksInput.Text.Trim();
            if ((decision != "Verified" && decision != "Not Verified") ||
                ((decision == "Not Verified" || isMissing) && String.IsNullOrWhiteSpace(remarks)) ||
                (isMissing && decision != "Not Verified") || remarks.Length > 2000)
            {
                ShowMessage("Select a valid decision; a not-verified decision requires remarks of at most 2,000 characters.");
                return;
            }

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    EnsureAssignedStage(connection, transaction, "Final_JE_CompletePrimaryVerification",
                        "tender.primary.verify");
                    using (SqlCommand command = new SqlCommand(
                        @"IF @IsMissing=1
                          BEGIN
                              IF EXISTS
                              (
                                  SELECT 1 FROM dbo.TenderDocumentVerificationHistory WITH (UPDLOCK,HOLDLOCK)
                                  WHERE TenderId=@TenderId AND ContractorId=@ContractorId
                                    AND TechnicalBidDocumentId IS NULL AND DocumentType=@DocumentType
                                    AND ReviewStage=N'Primary'
                              )
                                  THROW 51112,'A primary decision has already been recorded for this required document.',1;
                              IF EXISTS
                              (
                                  SELECT 1 FROM dbo.TechnicalBidDocuments d
                                  INNER JOIN dbo.TechnicalBid b ON b.TechnicalBidId=d.TechnicalBidId
                                  WHERE b.TenderId=@TenderId AND b.ContractorId=@ContractorId
                                    AND b.Status=N'Submitted' AND d.DocumentType=@DocumentType
                              )
                                  THROW 51113,'The required document was uploaded after the page was loaded.',1;
                              INSERT INTO dbo.TenderDocumentVerificationHistory
                                (TenderId,ContractorId,TechnicalBidDocumentId,FileName,FilePath,DocumentType,
                                 ReviewStage,VerificationStatus,ReviewerId,ReviewerRoleId,Remarks)
                              SELECT @TenderId,b.ContractorId,NULL,N'(not submitted)',N'',r.DocumentType,
                                     N'Primary',N'Not Verified',@UserId,@RoleId,@Remarks
                              FROM dbo.TechnicalBid b
                              INNER JOIN dbo.TenderRequiredBidDocuments r ON r.TenderId=b.TenderId
                              WHERE b.TenderId=@TenderId AND b.ContractorId=@ContractorId
                                AND b.Status=N'Submitted' AND r.DocumentType=@DocumentType
                                AND NOT EXISTS
                                (
                                    SELECT 1 FROM dbo.TechnicalBidDocuments d
                                    WHERE d.TechnicalBidId=b.TechnicalBidId AND d.DocumentType=r.DocumentType
                                );
                          END
                          ELSE
                          BEGIN
                              IF EXISTS
                              (
                                  SELECT 1 FROM dbo.TenderDocumentVerificationHistory WITH (UPDLOCK,HOLDLOCK)
                                  WHERE TenderId=@TenderId AND TechnicalBidDocumentId=@DocumentId
                                    AND ReviewStage=N'Primary'
                              )
                                  THROW 51112,'A primary document decision has already been recorded.',1;
                              INSERT INTO dbo.TenderDocumentVerificationHistory
                                (TenderId,ContractorId,TechnicalBidDocumentId,FileName,FilePath,DocumentType,
                                 ReviewStage,VerificationStatus,ReviewerId,ReviewerRoleId,Remarks)
                              SELECT @TenderId,b.ContractorId,d.TechnicalBidDocumentId,d.FileName,d.FilePath,d.DocumentType,
                                     N'Primary',@Decision,@UserId,@RoleId,@Remarks
                              FROM dbo.TechnicalBidDocuments d
                              INNER JOIN dbo.TechnicalBid b ON b.TechnicalBidId=d.TechnicalBidId
                              WHERE d.TechnicalBidDocumentId=@DocumentId AND b.TenderId=@TenderId
                                AND b.ContractorId=@ContractorId AND b.Status=N'Submitted';
                          END;",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        ModuleDb.AddParameter(command, "@DocumentId", SqlDbType.Int, documentId);
                        ModuleDb.AddParameter(command, "@ContractorId", SqlDbType.Int, contractorId);
                        AddText(command, "@DocumentType", SqlDbType.NVarChar, documentType, 160);
                        ModuleDb.AddParameter(command, "@IsMissing", SqlDbType.Bit, isMissing);
                        AddText(command, "@Decision", SqlDbType.NVarChar, decision, 20);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, CurrentRoleId);
                        AddText(command, "@Remarks", SqlDbType.NVarChar, remarks, 2000);
                        if (command.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("The selected document is no longer part of this tender.");
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "PrimaryDocumentVerification",
                        "TechnicalBidDocument", isMissing ? (int?)null : documentId,
                        documentType + " — " + decision + ": " + remarks, Request.UserHostAddress);
                    transaction.Commit();
                }

                Message.Text = "<div class=\"message success\">Document decision recorded.</div>";
                BindPrimaryVerificationAfterPostback();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Primary document decision could not be recorded.", ex);
                ShowMessage(ex.Number == 51112
                    ? "A primary decision has already been recorded for this document."
                    : ex.Number == 51113
                        ? "The required document was uploaded after the page was loaded. Refresh and verify the uploaded file."
                    : "The document decision could not be saved.");
            }
            catch (InvalidOperationException ex) { ShowMessage(ModuleDb.Html(ex.Message)); }
        }

        private void BindPrimaryVerificationAfterPostback()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                    BindPrimaryVerification(connection);
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Primary verification data could not be refreshed.", ex);
                ShowMessage("The verification was saved, but the page could not refresh. Reload it to view the saved result.");
            }
        }

        private void ProcessStage(string expectedStageName, string requiredPermission, string newStatus,
            string remarks, DateTime? publicationStartUtc, DateTime? publicationCloseUtc)
        {
            if (!HasPermission(requiredPermission) || remarks.Length > 2000)
            {
                ShowMessage("Your role is not authorized for this action, or the remarks are too long.");
                return;
            }

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    int stageId, assignedRoleId;
                    int? assignedUserId;
                    int departmentId;
                    string previousStatus;
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT t.CurrentStatus,s.StageId,s.StatusOnPending,s.StatusOnApprove,
                                 a.AssignedRoleId,a.AssignedUserId,a.DepartmentId
                          FROM dbo.Tender t WITH (UPDLOCK,HOLDLOCK)
                          INNER JOIN dbo.TenderWorkflowAssignments a WITH (UPDLOCK,HOLDLOCK)
                            ON a.TenderId=t.TenderId AND a.IsCurrent=1
                          INNER JOIN dbo.WorkflowStages s ON s.StageId=a.StageId AND s.WorkflowVersion=2 AND s.IsActive=1
                          WHERE t.TenderId=@TenderId AND t.WorkflowVersion=2 AND t.IsDeleted=0
                            AND s.StageName=@StageName;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        AddText(command, "@StageName", SqlDbType.NVarChar, expectedStageName, 100);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                                throw new InvalidOperationException("The tender is no longer at this workflow stage.");
                            previousStatus = Convert.ToString(reader["CurrentStatus"]);
                            if (!String.Equals(previousStatus, Convert.ToString(reader["StatusOnPending"]), StringComparison.Ordinal))
                                throw new InvalidOperationException("Tender status and workflow stage are inconsistent. Contact support.");
                            if (!String.Equals(Convert.ToString(reader["StatusOnApprove"]), newStatus, StringComparison.Ordinal))
                                throw new InvalidOperationException("The requested status transition is not configured.");
                            assignedRoleId = Convert.ToInt32(reader["AssignedRoleId"]);
                            assignedUserId = reader["AssignedUserId"] == DBNull.Value
                                ? (int?)null
                                : Convert.ToInt32(reader["AssignedUserId"]);
                            departmentId = Convert.ToInt32(reader["DepartmentId"]);
                            stageId = Convert.ToInt32(reader["StageId"]);
                        }
                    }
                    if (assignedRoleId != CurrentRoleId ||
                        (assignedUserId.HasValue && assignedUserId.Value != CurrentUserId) ||
                        (!IsSystemAdministrator && HasAnyRole("Junior Engineer (JE)") &&
                         (!CurrentDepartmentId.HasValue || CurrentDepartmentId.Value != departmentId)))
                        throw new InvalidOperationException("The tender is not assigned to your role, user, or department.");

                    if (expectedStageName == "Final_JE_SubmitTender")
                    {
                        EnsureTenderPreparationComplete(connection, transaction);
                        SaveRequiredBidDocumentTypes(connection, transaction);
                    }
                    if (expectedStageName == "Final_JE_PublishTender")
                        SavePublicationSchedule(connection, transaction, publicationStartUtc.Value, publicationCloseUtc.Value, remarks);
                    if (expectedStageName == "Final_JE_OpenTechnicalBids")
                        SaveTechnicalBidOpening(connection, transaction, remarks);
                    if (expectedStageName == "Final_JE_CompletePrimaryVerification")
                        EnsurePrimaryVerificationComplete(connection, transaction);

                    using (SqlCommand command = new SqlCommand(
                        @"UPDATE dbo.Tender SET CurrentStatus=@NewStatus,ModifiedBy=@UserId,ModifiedDate=SYSUTCDATETIME(),
                              SubmissionStartDate=CASE WHEN @StartUtc IS NULL THEN SubmissionStartDate ELSE @StartUtc END,
                              SubmissionDeadline=CASE WHEN @CloseUtc IS NULL THEN SubmissionDeadline ELSE @CloseUtc END
                          WHERE TenderId=@TenderId AND CurrentStatus=@PreviousStatus AND WorkflowVersion=2 AND IsDeleted=0;",
                        connection, transaction))
                    {
                        AddText(command, "@NewStatus", SqlDbType.NVarChar, newStatus, 100);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(command, "@StartUtc", SqlDbType.DateTime2,
                            publicationStartUtc.HasValue ? (object)publicationStartUtc.Value : null);
                        ModuleDb.AddParameter(command, "@CloseUtc", SqlDbType.DateTime2,
                            publicationCloseUtc.HasValue ? (object)publicationCloseUtc.Value : null);
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        AddText(command, "@PreviousStatus", SqlDbType.NVarChar, previousStatus, 100);
                        if (command.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("Tender status changed; refresh and try again.");
                    }

                    using (SqlCommand command = new SqlCommand(
                        @"UPDATE dbo.TenderWorkflowAssignments
                          SET IsCurrent=0,CompletedAtUtc=SYSUTCDATETIME()
                          WHERE TenderId=@TenderId AND IsCurrent=1 AND StageId=@StageId;",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        ModuleDb.AddParameter(command, "@StageId", SqlDbType.Int, stageId);
                        if (command.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("The workflow assignment changed; refresh and try again.");
                    }

                    int? nextStageId = null;
                    int? nextRoleId = null;
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT TOP (1) StageId,ApproverRoleId FROM dbo.WorkflowStages
                          WHERE WorkflowVersion=2 AND IsActive=1 AND StatusOnPending=@NewStatus
                            AND StageOrder>@CurrentOrder ORDER BY StageOrder;",
                        connection, transaction))
                    {
                        AddText(command, "@NewStatus", SqlDbType.NVarChar, newStatus, 100);
                        ModuleDb.AddParameter(command, "@CurrentOrder", SqlDbType.Int, GetStageOrder(connection, transaction, stageId));
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                nextStageId = Convert.ToInt32(reader["StageId"]);
                                nextRoleId = Convert.ToInt32(reader["ApproverRoleId"]);
                            }
                        }
                    }
                    if (!nextStageId.HasValue || !nextRoleId.HasValue)
                        throw new InvalidOperationException("The next workflow stage is not configured.");

                    int? nextAssignedUserId = nextRoleId.Value == CurrentRoleId &&
                        HasAnyRole("Junior Engineer (JE)") ? (int?)CurrentUserId : null;
                    using (SqlCommand command = new SqlCommand(
                        @"INSERT INTO dbo.TenderWorkflowAssignments
                            (TenderId,StageId,AssignedRoleId,AssignedUserId,DepartmentId,IsCurrent)
                          VALUES(@TenderId,@StageId,@RoleId,@AssignedUserId,@DepartmentId,1);",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        ModuleDb.AddParameter(command, "@StageId", SqlDbType.Int, nextStageId.Value);
                        ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, nextRoleId.Value);
                        ModuleDb.AddParameter(command, "@AssignedUserId", SqlDbType.Int,
                            nextAssignedUserId.HasValue ? (object)nextAssignedUserId.Value : null);
                        ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int, departmentId);
                        command.ExecuteNonQuery();
                    }

                    using (SqlCommand command = new SqlCommand(
                        @"INSERT INTO dbo.TenderWorkflowHistory
                            (TenderId,WorkflowVersion,StageId,PreviousStatus,NewStatus,ActionName,
                             UserId,RoleId,DepartmentId,Remarks)
                          VALUES(@TenderId,2,@StageId,@PreviousStatus,@NewStatus,@ActionName,
                                 @UserId,@RoleId,@DepartmentId,@Remarks);",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                        ModuleDb.AddParameter(command, "@StageId", SqlDbType.Int, stageId);
                        AddText(command, "@PreviousStatus", SqlDbType.NVarChar, previousStatus, 100);
                        AddText(command, "@NewStatus", SqlDbType.NVarChar, newStatus, 100);
                        AddText(command, "@ActionName", SqlDbType.NVarChar, expectedStageName, 100);
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                        ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, CurrentRoleId);
                        ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int, departmentId);
                        AddText(command, "@Remarks", SqlDbType.NVarChar, remarks, 2000);
                        command.ExecuteNonQuery();
                    }
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "WorkflowTransition", "Tender",
                        TenderId, previousStatus + " -> " + newStatus, Request.UserHostAddress);
                    transaction.Commit();
                }

                Response.Redirect("~/Pages/TenderWorkflow.aspx?id=" + TenderId.ToString(CultureInfo.InvariantCulture), false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Finalized workflow transition failed.", ex);
                ShowMessage("The workflow action could not be saved. Please retry or contact support.");
            }
            catch (InvalidOperationException ex)
            {
                ShowMessage(ex.Message);
            }
        }

        private static int GetStageOrder(SqlConnection connection, SqlTransaction transaction, int stageId)
        {
            using (SqlCommand command = new SqlCommand(
                "SELECT StageOrder FROM dbo.WorkflowStages WHERE StageId=@StageId;", connection, transaction))
            {
                ModuleDb.AddParameter(command, "@StageId", SqlDbType.Int, stageId);
                object value = command.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                    throw new InvalidOperationException("The current workflow stage is not configured.");
                return Convert.ToInt32(value);
            }
        }

            private void EnsureAssignedStage(SqlConnection connection, SqlTransaction transaction,
                string stageName, string requiredPermission)
            {
                if (!HasPermission(requiredPermission))
                    throw new InvalidOperationException("Your role is not authorized to perform this workflow action.");

                using (SqlCommand command = new SqlCommand(
                    @"SELECT COUNT(1)
                      FROM dbo.Tender t WITH (UPDLOCK,HOLDLOCK)
                      INNER JOIN dbo.TenderWorkflowAssignments a WITH (UPDLOCK,HOLDLOCK)
                        ON a.TenderId=t.TenderId AND a.IsCurrent=1
                      INNER JOIN dbo.WorkflowStages s ON s.StageId=a.StageId
                        AND s.WorkflowVersion=2 AND s.IsActive=1
                      WHERE t.TenderId=@TenderId AND t.WorkflowVersion=2 AND t.IsDeleted=0
                        AND s.StageName=@StageName AND t.CurrentStatus=s.StatusOnPending
                        AND a.AssignedRoleId=@RoleId
                        AND (a.AssignedUserId IS NULL OR a.AssignedUserId=@UserId)
                        AND (@Role<>N'Junior Engineer (JE)'
                             OR (@DepartmentId IS NOT NULL AND a.DepartmentId=@DepartmentId
                                 AND t.DepartmentId=@DepartmentId));",
                    connection, transaction))
                {
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    AddText(command, "@StageName", SqlDbType.NVarChar, stageName, 100);
                    ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, CurrentRoleId);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                    AddText(command, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
                    ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                        CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                    if (Convert.ToInt32(command.ExecuteScalar()) != 1)
                        throw new InvalidOperationException("The tender is no longer assigned to you at this workflow stage.");
                }
            }

            private void SaveTechnicalBidOpening(SqlConnection connection, SqlTransaction transaction, string remarks)
            {
                if (remarks.Length > 1000)
                    throw new InvalidOperationException("Opening remarks cannot exceed 1,000 characters.");

                DateTime originalClosingUtc;
                DateTime currentClosingUtc;
                using (SqlCommand command = new SqlCommand(
                    @"SELECT s.OriginalClosingUtc,s.CurrentClosingUtc
                      FROM dbo.Tender t WITH (UPDLOCK,HOLDLOCK)
                      INNER JOIN dbo.TenderPublicationSchedule s WITH (UPDLOCK,HOLDLOCK)
                        ON s.TenderId=t.TenderId
                      WHERE t.TenderId=@TenderId AND t.WorkflowVersion=2 AND t.IsDeleted=0
                        AND t.CurrentStatus=N'Tender Published'
                        AND s.PublicationStartUtc<=SYSUTCDATETIME()
                        AND s.CurrentClosingUtc<=SYSUTCDATETIME()
                        AND NOT EXISTS(SELECT 1 FROM dbo.TenderBidOpening o WITH (UPDLOCK,HOLDLOCK)
                                       WHERE o.TenderId=t.TenderId);",
                    connection, transaction))
                {
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new InvalidOperationException("The approved publication period must be closed, and no technical opening may already exist.");
                        originalClosingUtc = Convert.ToDateTime(reader["OriginalClosingUtc"], CultureInfo.InvariantCulture);
                        currentClosingUtc = Convert.ToDateTime(reader["CurrentClosingUtc"], CultureInfo.InvariantCulture);
                    }
                }

                using (SqlCommand command = new SqlCommand(
                    @"SELECT COUNT(1) FROM dbo.TenderRequiredBidDocuments
                      WHERE TenderId=@TenderId;", connection, transaction))
                {
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                        throw new InvalidOperationException("Configure the tender's required bidder-document checklist before opening technical bids.");
                }

                using (SqlCommand command = new SqlCommand(
                    @"SELECT COUNT(1) FROM dbo.TechnicalBid
                      WHERE TenderId=@TenderId AND Status=N'Submitted';", connection, transaction))
                {
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                        throw new InvalidOperationException("No technical bids were received. The tender cannot proceed without an approved no-bid or cancellation path.");
                }

                string operatorName = Convert.ToString(Session["FullName"]);
                if (String.IsNullOrWhiteSpace(operatorName))
                {
                    using (SqlCommand command = new SqlCommand(
                        "SELECT FullName FROM dbo.Users WHERE UserId=@UserId AND IsActive=1;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                        operatorName = Convert.ToString(command.ExecuteScalar());
                    }
                }
                if (String.IsNullOrWhiteSpace(operatorName) || operatorName.Length > 150)
                    throw new InvalidOperationException("The opening operator identity is unavailable or invalid.");

                using (SqlCommand command = new SqlCommand(
                    @"INSERT INTO dbo.TenderBidOpening
                        (TenderId,OriginalClosingUtc,CurrentClosingUtc,ActualOpeningUtc,OpenedBy,
                         OperatorName,Designation,OpeningRemarks,OpeningStatus)
                      VALUES(@TenderId,@OriginalClosingUtc,@CurrentClosingUtc,SYSUTCDATETIME(),@UserId,
                             @OperatorName,N'Junior Engineer (JE)',@Remarks,N'Opened');",
                    connection, transaction))
                {
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    ModuleDb.AddParameter(command, "@OriginalClosingUtc", SqlDbType.DateTime2, originalClosingUtc);
                    ModuleDb.AddParameter(command, "@CurrentClosingUtc", SqlDbType.DateTime2, currentClosingUtc);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                    AddText(command, "@OperatorName", SqlDbType.NVarChar, operatorName, 150);
                    AddText(command, "@Remarks", SqlDbType.NVarChar, remarks, 1000);
                    command.ExecuteNonQuery();
                }
            }

            private void EnsurePrimaryVerificationComplete(SqlConnection connection, SqlTransaction transaction)
            {
                int bidderCount;
                using (SqlCommand command = new SqlCommand(
                    "SELECT COUNT(1) FROM dbo.TechnicalBid WHERE TenderId=@TenderId AND Status=N'Submitted';",
                    connection, transaction))
                {
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    bidderCount = Convert.ToInt32(command.ExecuteScalar());
                }
                if (bidderCount == 0)
                    throw new InvalidOperationException("No submitted technical bids are available for primary verification.");

                using (SqlCommand command = new SqlCommand(
                    "SELECT COUNT(1) FROM dbo.TenderRequiredBidDocuments WHERE TenderId=@TenderId;",
                    connection, transaction))
                {
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                        throw new InvalidOperationException("The tender has no configured required bidder-document checklist.");
                }

                using (SqlCommand command = new SqlCommand(
                    @"SELECT COUNT(1)
                      FROM dbo.TechnicalBid b
                      WHERE b.TenderId=@TenderId AND b.Status=N'Submitted'
                        AND
                        (
                            NOT EXISTS
                            (
                                SELECT 1 FROM dbo.TenderVerificationReviews r
                                WHERE r.TenderId=b.TenderId AND r.ContractorId=b.ContractorId
                                  AND r.ReviewStage=N'Primary' AND r.Decision=N'Completed'
                            )
                            OR
                            (
                                NOT EXISTS(SELECT 1 FROM dbo.TechnicalBidDocuments d
                                           WHERE d.TechnicalBidId=b.TechnicalBidId)
                                AND NOT EXISTS
                                (
                                    SELECT 1 FROM dbo.TenderVerificationReviews r
                                    WHERE r.TenderId=b.TenderId AND r.ContractorId=b.ContractorId
                                      AND r.ReviewStage=N'Primary' AND r.Decision=N'Completed'
                                      AND NULLIF(LTRIM(RTRIM(r.Remarks)),N'') IS NOT NULL
                                )
                            )
                            OR EXISTS
                            (
                                SELECT 1 FROM dbo.TechnicalBidDocuments d
                                WHERE d.TechnicalBidId=b.TechnicalBidId
                                  AND NOT EXISTS
                                  (
                                      SELECT 1 FROM dbo.TenderDocumentVerificationHistory h
                                      WHERE h.TenderId=b.TenderId AND h.ContractorId=b.ContractorId
                                        AND h.TechnicalBidDocumentId=d.TechnicalBidDocumentId
                                        AND h.ReviewStage=N'Primary'
                                  )
                            )
                            OR EXISTS
                            (
                                    SELECT 1 FROM dbo.TenderRequiredBidDocuments requiredDocument
                                    WHERE requiredDocument.TenderId=b.TenderId
                                      AND
                                      (
                                          (
                                              NOT EXISTS
                                              (
                                                  SELECT 1 FROM dbo.TechnicalBidDocuments d
                                                  WHERE d.TechnicalBidId=b.TechnicalBidId
                                                    AND d.DocumentType=requiredDocument.DocumentType
                                              )
                                              AND NOT EXISTS
                                              (
                                                  SELECT 1 FROM dbo.TenderDocumentVerificationHistory h
                                                  WHERE h.TenderId=b.TenderId AND h.ContractorId=b.ContractorId
                                                    AND h.TechnicalBidDocumentId IS NULL
                                                    AND h.DocumentType=requiredDocument.DocumentType
                                                    AND h.ReviewStage=N'Primary'
                                              )
                                          )
                                          OR EXISTS
                                          (
                                              SELECT 1 FROM dbo.TechnicalBidDocuments d
                                              WHERE d.TechnicalBidId=b.TechnicalBidId
                                                AND d.DocumentType=requiredDocument.DocumentType
                                                AND NOT EXISTS
                                                (
                                                    SELECT 1 FROM dbo.TenderDocumentVerificationHistory h
                                                    WHERE h.TenderId=b.TenderId AND h.ContractorId=b.ContractorId
                                                      AND h.TechnicalBidDocumentId=d.TechnicalBidDocumentId
                                                      AND h.ReviewStage=N'Primary'
                                                )
                                          )
                                      )
                            )
                        );",
                    connection, transaction))
                {
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    if (Convert.ToInt32(command.ExecuteScalar()) != 0)
                        throw new InvalidOperationException("Record a bidder review for every bidder, explain any bidder with no uploaded files, and decide every uploaded technical document before completing verification.");
                }
            }

        private void EnsureTenderPreparationComplete(SqlConnection connection, SqlTransaction transaction)
        {
            using (SqlCommand command = new SqlCommand(
                @"SELECT (SELECT COUNT(*) FROM dbo.BOQ WHERE TenderId=@TenderId) AS BOQCount,
                         COUNT(CASE WHEN DocumentType=N'Tender Proposal' THEN 1 END) AS ProposalCount,
                         COUNT(CASE WHEN DocumentType=N'Tender Document' THEN 1 END) AS TenderDocumentCount,
                         COUNT(CASE WHEN DocumentType=N'Technical Supporting Document' THEN 1 END) AS TechnicalDocumentCount,
                         COUNT(CASE WHEN DocumentType=N'Financial Supporting Document' THEN 1 END) AS FinancialDocumentCount
                  FROM dbo.TenderDocuments WHERE TenderId=@TenderId;", connection, transaction))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read()) throw new InvalidOperationException("Tender preparation could not be verified.");
                    if (Convert.ToInt32(reader["BOQCount"]) == 0 ||
                        Convert.ToInt32(reader["ProposalCount"]) == 0 ||
                        Convert.ToInt32(reader["TenderDocumentCount"]) == 0 ||
                        Convert.ToInt32(reader["TechnicalDocumentCount"]) == 0 ||
                        Convert.ToInt32(reader["FinancialDocumentCount"]) == 0)
                        throw new InvalidOperationException("Before submitting, add a BOQ item and all four required tender document categories.");
                }
            }
        }

        private void SaveRequiredBidDocumentTypes(SqlConnection connection, SqlTransaction transaction)
        {
            if (String.IsNullOrWhiteSpace(RequiredBidDocumentsText.Text) ||
                RequiredBidDocumentsText.Text.Length > 8000)
                throw new InvalidOperationException("Enter at least one required bidder document type, one per line.");

            List<string> documentTypes = new List<string>();
            HashSet<string> uniqueDocumentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] lines = RequiredBidDocumentsText.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string documentType = line.Trim();
                if (documentType.Length == 0) continue;
                if (documentType.Length > 160)
                    throw new InvalidOperationException("Each required document type must be 160 characters or fewer.");
                if (!uniqueDocumentTypes.Add(documentType))
                    throw new InvalidOperationException("Required document types cannot be duplicated.");
                documentTypes.Add(documentType);
            }
            if (documentTypes.Count == 0)
                throw new InvalidOperationException("Enter at least one required bidder document type, one per line.");

            using (SqlCommand command = new SqlCommand(
                "DELETE FROM dbo.TenderRequiredBidDocuments WHERE TenderId=@TenderId;",
                connection, transaction))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                command.ExecuteNonQuery();
            }
            foreach (string documentType in documentTypes)
            {
                using (SqlCommand command = new SqlCommand(
                    @"INSERT INTO dbo.TenderRequiredBidDocuments(TenderId,DocumentType,CreatedBy)
                      VALUES(@TenderId,@DocumentType,@UserId);", connection, transaction))
                {
                    ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                    AddText(command, "@DocumentType", SqlDbType.NVarChar, documentType, 160);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void SavePublicationSchedule(SqlConnection connection, SqlTransaction transaction,
            DateTime startUtc, DateTime closeUtc, string remarks)
        {
            if (startUtc <= DateTime.UtcNow || closeUtc <= startUtc)
                throw new InvalidOperationException("The publication period is invalid or has already started.");
            decimal publicationDays = Decimal.Round((decimal)(closeUtc - startUtc).TotalDays, 2);
            using (SqlCommand command = new SqlCommand(
                @"IF EXISTS(SELECT 1 FROM dbo.TenderPublicationSchedule WITH (UPDLOCK,HOLDLOCK) WHERE TenderId=@TenderId)
                      THROW 51020,'A publication schedule already exists.',1;
                  INSERT INTO dbo.TenderPublicationSchedule
                    (TenderId,PublicationStartUtc,OriginalClosingUtc,CurrentClosingUtc,PublicationPeriodDays,
                     Remarks,ApprovedBy,ApprovedAtUtc,ModifiedAtUtc)
                  VALUES(@TenderId,@StartUtc,@CloseUtc,@CloseUtc,@Duration,@Remarks,@UserId,
                         SYSUTCDATETIME(),SYSUTCDATETIME());",
                connection, transaction))
            {
                ModuleDb.AddParameter(command, "@TenderId", SqlDbType.Int, TenderId);
                ModuleDb.AddParameter(command, "@StartUtc", SqlDbType.DateTime2, startUtc);
                ModuleDb.AddParameter(command, "@CloseUtc", SqlDbType.DateTime2, closeUtc);
                ModuleDb.AddParameter(command, "@Duration", SqlDbType.Decimal, publicationDays);
                AddText(command, "@Remarks", SqlDbType.NVarChar, remarks, 2000);
                ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                command.ExecuteNonQuery();
            }
        }

        private static bool TryParseIndiaDateTime(string value, out DateTime utc)
        {
            utc = DateTime.MinValue;
            DateTime localTime;
            if (!DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out localTime)) return false;
            TimeZoneInfo indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            utc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified), indiaTimeZone);
            return true;
        }

        private void DenyAccess()
        {
            Response.StatusCode = 403;
            Response.TrySkipIisCustomErrors = true;
            Response.Redirect("~/Pages/Error.aspx?code=403", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void ShowMessage(string text)
        {
            Message.Text = "<div class=\"message\">" + text + "</div>";
        }
    }
}
