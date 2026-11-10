using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class DepartmentDashboard : ModulePage
    {
        protected Literal Message, TotalTenders, ApprovedTenders, PendingCount, RejectedTenders, UnderReviewTenders, RecentHeading,
            LifecyclePendingApprovals, LifecycleAwaitingPublication, LifecyclePublished, LifecycleExtensions,
            LifecycleBidOpenings, LifecycleAwaitingEvaluation, LifecycleUnderEvaluation;
        protected HyperLink NewTenderLink;
        protected Panel PendingApprovalsPanel, LifecycleSummaryPanel;
        protected System.Web.UI.WebControls.PlaceHolder LifecycleReviewSummaryPanel;
        protected GridView PendingGrid, RecentGrid, CategoryStatsGrid, UpcomingGrid;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                bool isRespOfficer = HasAnyRole("DepartmentOfficer");
                NewTenderLink.Visible = HasAnyRole("DepartmentOfficer", "Admin");
                PendingApprovalsPanel.Visible = !isRespOfficer;
                LifecycleSummaryPanel.Visible = !HasAnyRole("Contractor");
                LifecycleReviewSummaryPanel.Visible = HasAnyRole("CAFOOfficer", "TenderSection", "TenderCommittee", "Admin");
                RecentHeading.Text = isRespOfficer ? "My recent tenders" : "Recently updated tenders";
                LoadDashboard();
            }
        }

        private void LoadDashboard()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    bool isRespOfficer = HasAnyRole("DepartmentOfficer");
                    string ownerFilter = isRespOfficer ? " AND t.CreatedBy=@CurrentUserId" : String.Empty;
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT COUNT(*) AS TotalTenders,
                                 SUM(CASE WHEN " + TenderStatusBuckets.Approved + @" THEN 1 ELSE 0 END) AS ApprovedTenders,
                                 SUM(CASE WHEN " + TenderStatusBuckets.Pending + @" THEN 1 ELSE 0 END) AS PendingCount,
                                 SUM(CASE WHEN " + TenderStatusBuckets.Rejected + @" THEN 1 ELSE 0 END) AS RejectedTenders,
                                 SUM(CASE WHEN " + TenderStatusBuckets.UnderReview + @" THEN 1 ELSE 0 END) AS UnderReviewTenders
                          FROM dbo.Tender t WHERE t.IsDeleted=0" + ownerFilter + ";", connection))
                    {
                        if (isRespOfficer)
                            ModuleDb.AddParameter(command, "@CurrentUserId", SqlDbType.Int, CurrentUserId);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                TotalTenders.Text = Convert.ToString(reader["TotalTenders"]);
                                ApprovedTenders.Text = Convert.ToString(reader["ApprovedTenders"] == DBNull.Value ? 0 : reader["ApprovedTenders"]);
                                PendingCount.Text = Convert.ToString(reader["PendingCount"] == DBNull.Value ? 0 : reader["PendingCount"]);
                                RejectedTenders.Text = Convert.ToString(reader["RejectedTenders"] == DBNull.Value ? 0 : reader["RejectedTenders"]);
                                UnderReviewTenders.Text = Convert.ToString(reader["UnderReviewTenders"] == DBNull.Value ? 0 : reader["UnderReviewTenders"]);
                            }
                        }
                    }

                    if (!isRespOfficer)
                    {
                        using (SqlCommand command = new SqlCommand(
                            @"SELECT TOP (5) t.TenderId, t.TenderNumber, t.Title, d.DepartmentName, t.CurrentStatus
                              FROM dbo.Tender t INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                              WHERE t.IsDeleted=0 AND " + RoleStatusFilter(CurrentRole) +
                            " ORDER BY t.CreatedDate DESC;", connection))
                        {
                            AddRoleStatusParameters(command, CurrentRole);
                            PendingGrid.DataSource = ReadTable(command);
                            PendingGrid.DataBind();
                        }
                    }

                    BindLifecycleSummary(connection, ownerFilter, isRespOfficer);
                    BindDashboardTables(connection, isRespOfficer, ownerFilter);
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Could not load the department dashboard.", ex);
                Message.Text = "<div class=\"message\">Dashboard data could not be loaded. Please retry or contact support.</div>";
            }
        }

        private void BindLifecycleSummary(SqlConnection connection, string ownerFilter, bool isRespOfficer)
        {
            bool isAdmin = HasAnyRole("Admin");
            string departmentFilter = isAdmin ? String.Empty :
                @" AND (@DepartmentId IS NULL OR t.DepartmentId=@DepartmentId)
                   AND (NOT EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                   WHERE scopeUser.UserId=@ScopeUserId AND scopeUser.DepartmentId IS NOT NULL)
                        OR EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                  WHERE scopeUser.UserId=@ScopeUserId AND scopeUser.DepartmentId=t.DepartmentId))";
            string tenderScope = "t.IsDeleted=0" + ownerFilter + departmentFilter;
            string[] pendingStatuses = WorkflowRules.PendingStatusesForRole(CurrentRole);
            string[] pendingParameters = new string[pendingStatuses.Length];
            for (int i = 0; i < pendingStatuses.Length; i++) pendingParameters[i] = "@LifecycleStatus" + i;
            string pendingPredicate = pendingStatuses.Length == 0
                ? "1=0"
                : "t.CurrentStatus IN (" + String.Join(",", pendingParameters) + ")";

            string sql = @"SELECT
                (SELECT COUNT(*) FROM dbo.Tender t WHERE " + tenderScope + " AND " + pendingPredicate + @") AS PendingApprovals,
                (SELECT COUNT(*) FROM dbo.Tender t LEFT JOIN dbo.TenderPublicationSchedule s ON s.TenderId=t.TenderId
                 WHERE " + tenderScope + @" AND t.CurrentStatus=N'Tender Published'
                   AND (s.TenderId IS NULL OR s.PublicationStartUtc>SYSUTCDATETIME())) AS AwaitingPublication,
                (SELECT COUNT(*) FROM dbo.Tender t INNER JOIN dbo.TenderPublicationSchedule s ON s.TenderId=t.TenderId
                 WHERE " + tenderScope + @" AND t.CurrentStatus=N'Tender Published'
                   AND s.PublicationStartUtc<=SYSUTCDATETIME() AND s.CurrentClosingUtc>SYSUTCDATETIME()) AS CurrentlyPublished,
                (SELECT COUNT(*) FROM dbo.TenderExtensionRequests e INNER JOIN dbo.Tender t ON t.TenderId=e.TenderId
                 WHERE " + tenderScope + @" AND e.ReviewStatus=N'Pending') AS ExtensionRequests,
                (SELECT COUNT(*) FROM dbo.Tender t INNER JOIN dbo.TenderPublicationSchedule s ON s.TenderId=t.TenderId
                 WHERE " + tenderScope + @" AND t.CurrentStatus=N'Tender Published'
                   AND s.CurrentClosingUtc<=SYSUTCDATETIME()
                   AND NOT EXISTS(SELECT 1 FROM dbo.TenderBidOpening o WHERE o.TenderId=t.TenderId)) AS PendingBidOpenings,
                (SELECT COUNT(*) FROM dbo.TenderEvaluation e INNER JOIN dbo.Tender t ON t.TenderId=e.TenderId
                 WHERE " + tenderScope + @" AND e.EvaluationStatus=N'Awaiting Evaluation') AS AwaitingEvaluation,
                (SELECT COUNT(*) FROM dbo.TenderEvaluation e INNER JOIN dbo.Tender t ON t.TenderId=e.TenderId
                 WHERE " + tenderScope + @" AND e.EvaluationStatus=N'Under Evaluation') AS UnderEvaluation;";
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                if (isRespOfficer)
                    ModuleDb.AddParameter(command, "@CurrentUserId", SqlDbType.Int, CurrentUserId);
                if (!isAdmin)
                {
                    ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                        CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                    ModuleDb.AddParameter(command, "@ScopeUserId", SqlDbType.Int, CurrentUserId);
                }
                for (int i = 0; i < pendingStatuses.Length; i++)
                    ModuleDb.AddParameter(command, pendingParameters[i], SqlDbType.NVarChar, pendingStatuses[i], 50);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return;
                    LifecyclePendingApprovals.Text = Convert.ToString(reader["PendingApprovals"]);
                    LifecycleAwaitingPublication.Text = Convert.ToString(reader["AwaitingPublication"]);
                    LifecyclePublished.Text = Convert.ToString(reader["CurrentlyPublished"]);
                    LifecycleExtensions.Text = Convert.ToString(reader["ExtensionRequests"]);
                    LifecycleBidOpenings.Text = Convert.ToString(reader["PendingBidOpenings"]);
                    LifecycleAwaitingEvaluation.Text = Convert.ToString(reader["AwaitingEvaluation"]);
                    LifecycleUnderEvaluation.Text = Convert.ToString(reader["UnderEvaluation"]);
                }
            }
        }

        private void BindDashboardTables(SqlConnection connection, bool isRespOfficer, string ownerFilter)
        {
            using (SqlCommand command = new SqlCommand(
                @"SELECT TOP (5) t.TenderId, t.TenderNumber, t.Title, t.TenderType, d.DepartmentName,
                         COALESCE(u.FullName,N'') AS AssignedOfficer,
                         (SELECT MIN(ah.ApprovalDate) FROM dbo.ApprovalHistory ah
                          WHERE ah.TenderId=t.TenderId AND ah.NewStatus=N'Tender Published') AS PublicationDate,
                         t.SubmissionStartDate,t.SubmissionDeadline,t.CurrentStatus,t.CreatedDate
                  FROM dbo.Tender t INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                  LEFT JOIN dbo.Users u ON u.UserId=t.CreatedBy
                  WHERE t.IsDeleted=0" + ownerFilter + " ORDER BY t.CreatedDate DESC;", connection))
            {
                if (isRespOfficer)
                    ModuleDb.AddParameter(command, "@CurrentUserId", SqlDbType.Int, CurrentUserId);
                RecentGrid.DataSource = ReadTable(command);
                RecentGrid.DataBind();
            }

            using (SqlCommand command = new SqlCommand(
                @"SELECT t.TenderType AS Category,COUNT(*) AS TenderCount
                  FROM dbo.Tender t WHERE t.IsDeleted=0" + ownerFilter +
                " GROUP BY t.TenderType ORDER BY TenderCount DESC,t.TenderType;", connection))
            {
                if (isRespOfficer)
                    ModuleDb.AddParameter(command, "@CurrentUserId", SqlDbType.Int, CurrentUserId);
                CategoryStatsGrid.DataSource = ReadTable(command);
                CategoryStatsGrid.DataBind();
            }

            using (SqlCommand command = new SqlCommand(
                @"SELECT TOP (5) t.TenderId,t.TenderNumber,t.Title,t.TenderType,t.SubmissionStartDate,t.SubmissionDeadline,
                         COALESCE(u.FullName,N'') AS AssignedOfficer,
                         d.DepartmentName,t.CurrentStatus
                  FROM dbo.Tender t INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                  LEFT JOIN dbo.Users u ON u.UserId=t.CreatedBy
                  WHERE t.IsDeleted=0 AND t.SubmissionDeadline>=SYSUTCDATETIME()
                    AND t.CurrentStatus<>N'Draft'" + ownerFilter +
                " ORDER BY t.SubmissionDeadline,t.TenderId;", connection))
            {
                if (isRespOfficer)
                    ModuleDb.AddParameter(command, "@CurrentUserId", SqlDbType.Int, CurrentUserId);
                UpcomingGrid.DataSource = ReadTable(command);
                UpcomingGrid.DataBind();
            }
        }

        private static DataTable ReadTable(SqlCommand command)
        {
            DataTable table = new DataTable();
            using (SqlDataAdapter adapter = new SqlDataAdapter(command)) adapter.Fill(table);
            return table;
        }

        private static string RoleStatusFilter(string role)
        {
            string[] statuses = WorkflowRules.PendingStatusesForRole(role);
            if (statuses.Length == 0) return "1=0";
            string[] placeholders = new string[statuses.Length];
            for (int i = 0; i < statuses.Length; i++) placeholders[i] = "@PendingStatus" + i;
            return "t.CurrentStatus IN (" + String.Join(",", placeholders) + ")";
        }

        private static void AddRoleStatusParameters(SqlCommand command, string role)
        {
            string[] statuses = WorkflowRules.PendingStatusesForRole(role);
            for (int i = 0; i < statuses.Length; i++)
                ModuleDb.AddParameter(command, "@PendingStatus" + i, System.Data.SqlDbType.NVarChar, statuses[i], 50);
        }
    }
}
