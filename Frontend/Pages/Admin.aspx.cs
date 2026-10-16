using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class AdminDashboard : ModulePage
    {
        private const int FeedItemsPerPage = 10;
        private const string FeedPageIndexKey = "AdminDashboardFeedPageIndex";

        protected Literal Message, TotalTenders, ApprovedTenders, PendingTenders, RejectedTenders, UnderReviewTenders,
            TotalContractors, ActiveContractors, TotalUsers, ActiveUsers, InactiveContractors, InactiveUsers, Departments, PageSummary;
        protected GridView DashboardFeedGrid, CategoryStatsGrid, UpcomingDeadlinesGrid, RecentTendersGrid;
        protected LinkButton PreviousPageButton, NextPageButton;
        protected Repeater PageNumbers;

        private int CurrentPage
        {
            get { return ViewState[FeedPageIndexKey] is int ? (int)ViewState[FeedPageIndexKey] : 1; }
            set { ViewState[FeedPageIndexKey] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequireRoles("Admin")) return;
            if (!IsPostBack) LoadDashboard();
        }

        private void LoadDashboard()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT COUNT(*) AS TotalTenders,
                            SUM(CASE WHEN " + TenderStatusBuckets.Approved + @" THEN 1 ELSE 0 END) AS ApprovedTenders,
                            SUM(CASE WHEN " + TenderStatusBuckets.Pending + @" THEN 1 ELSE 0 END) AS PendingTenders,
                            SUM(CASE WHEN " + TenderStatusBuckets.Rejected + @" THEN 1 ELSE 0 END) AS RejectedTenders,
                            SUM(CASE WHEN " + TenderStatusBuckets.UnderReview + @" THEN 1 ELSE 0 END) AS UnderReviewTenders
                          FROM dbo.Tender t WHERE t.IsDeleted=0;
                          SELECT
                            (SELECT COUNT(*) FROM dbo.Contractors) AS TotalContractors,
                            (SELECT COUNT(*) FROM dbo.Contractors WHERE IsActive=1) AS ActiveContractors,
                            (SELECT COUNT(*) FROM dbo.Users) AS TotalUsers,
                            (SELECT COUNT(*) FROM dbo.Users WHERE IsActive=1) AS ActiveUsers,
                            (SELECT COUNT(*) FROM dbo.Users WHERE IsActive=0) AS InactiveUsers,
                            (SELECT COUNT(*) FROM dbo.Contractors WHERE IsActive=0) AS InactiveContractors,
                            (SELECT COUNT(*) FROM dbo.Departments WHERE IsActive=1) AS Departments;", connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                TotalTenders.Text = Convert.ToString(reader["TotalTenders"]);
                                ApprovedTenders.Text = Convert.ToString(reader["ApprovedTenders"] == DBNull.Value ? 0 : reader["ApprovedTenders"]);
                                PendingTenders.Text = Convert.ToString(reader["PendingTenders"] == DBNull.Value ? 0 : reader["PendingTenders"]);
                                RejectedTenders.Text = Convert.ToString(reader["RejectedTenders"] == DBNull.Value ? 0 : reader["RejectedTenders"]);
                                UnderReviewTenders.Text = Convert.ToString(reader["UnderReviewTenders"] == DBNull.Value ? 0 : reader["UnderReviewTenders"]);
                            }
                            reader.NextResult();
                            if (reader.Read())
                            {
                                TotalContractors.Text = Convert.ToString(reader["TotalContractors"]);
                                ActiveContractors.Text = Convert.ToString(reader["ActiveContractors"]);
                                TotalUsers.Text = Convert.ToString(reader["TotalUsers"]);
                                ActiveUsers.Text = Convert.ToString(reader["ActiveUsers"]);
                                InactiveUsers.Text = Convert.ToString(reader["InactiveUsers"]);
                                InactiveContractors.Text = Convert.ToString(reader["InactiveContractors"]);
                                Departments.Text = Convert.ToString(reader["Departments"]);
                            }
                        }
                    }

                    BindOverviewTables(connection);
                    BindDashboardFeed(connection);
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Admin dashboard data could not be loaded.", ex);
                Message.Text = "<div class=\"message\">Dashboard data could not be loaded. Please retry or contact support.</div>";
            }
        }

        private void BindOverviewTables(SqlConnection connection)
        {
            using (SqlCommand command = new SqlCommand(
                @"SELECT t.TenderType AS Category,COUNT(*) AS TenderCount
                  FROM dbo.Tender t WHERE t.IsDeleted=0
                  GROUP BY t.TenderType ORDER BY TenderCount DESC,t.TenderType;", connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                DataTable data = new DataTable();
                adapter.Fill(data);
                CategoryStatsGrid.DataSource = data;
                CategoryStatsGrid.DataBind();
            }

            using (SqlCommand command = new SqlCommand(
                @"SELECT TOP (5) t.TenderId,t.TenderNumber,t.Title,t.TenderType,d.DepartmentName,
                         COALESCE(u.FullName,N'') AS AssignedOfficer,
                         (SELECT MIN(ah.ApprovalDate) FROM dbo.ApprovalHistory ah
                          WHERE ah.TenderId=t.TenderId AND ah.NewStatus=N'Tender Published') AS PublicationDate,
                         t.SubmissionStartDate,t.SubmissionDeadline,t.CurrentStatus
                  FROM dbo.Tender t INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                  LEFT JOIN dbo.Users u ON u.UserId=t.CreatedBy
                  WHERE t.IsDeleted=0 ORDER BY t.CreatedDate DESC,t.TenderId DESC;", connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                DataTable data = new DataTable();
                adapter.Fill(data);
                RecentTendersGrid.DataSource = data;
                RecentTendersGrid.DataBind();
            }

            using (SqlCommand command = new SqlCommand(
                @"SELECT TOP (5) t.TenderId,t.TenderNumber,t.Title,t.TenderType,d.DepartmentName,
                         COALESCE(u.FullName,N'') AS AssignedOfficer,
                         t.SubmissionStartDate,t.SubmissionDeadline,t.CurrentStatus
                  FROM dbo.Tender t INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                  LEFT JOIN dbo.Users u ON u.UserId=t.CreatedBy
                  WHERE t.IsDeleted=0 AND t.SubmissionDeadline>=SYSUTCDATETIME()
                    AND t.CurrentStatus<>N'Draft'
                  ORDER BY t.SubmissionDeadline,t.TenderId;", connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                DataTable data = new DataTable();
                adapter.Fill(data);
                UpcomingDeadlinesGrid.DataSource = data;
                UpcomingDeadlinesGrid.DataBind();
            }
        }

        private void BindDashboardFeed(SqlConnection connection)
        {
            const string feedSource = @"
                SELECT N'Tender' AS FeedType,t.TenderId,
                       COALESCE(t.ModifiedDate,t.CreatedDate) AS ActivityDate,
                       CONCAT(t.TenderNumber,N' - ',t.Title) AS Area,
                       CASE WHEN t.ModifiedDate IS NULL THEN N'Tender created' ELSE N'Tender updated' END AS Details,
                       COALESCE(NULLIF(u.FullName,N''),N'System / unknown') AS Actor,
                       t.CurrentStatus,
                       CAST(t.TenderId AS BIGINT) AS SortId
                FROM dbo.Tender t
                LEFT JOIN dbo.Users u ON u.UserId=COALESCE(t.ModifiedBy,t.CreatedBy)
                WHERE t.IsDeleted=0
                UNION ALL
                SELECT N'Activity' AS FeedType,CAST(NULL AS INT) AS TenderId,a.CreatedDate AS ActivityDate,
                       a.EntityName AS Area,a.Action + CASE WHEN a.Details IS NULL THEN N''
                           ELSE N' - ' + a.Details END AS Details,
                       COALESCE(NULLIF(u.FullName,N''),N'System / unknown') AS Actor,
                       CAST(NULL AS NVARCHAR(50)) AS CurrentStatus,
                       a.LogId AS SortId
                FROM dbo.AuditLog a
                LEFT JOIN dbo.Users u ON u.UserId=a.UserId
                WHERE a.EntityName IN (N'Tender',N'TenderDocument',N'BOQItem',N'TechnicalBid',N'FinancialBid')
                  AND NOT (a.EntityName=N'Tender' AND a.Action IN (N'Create',N'Update'))";

            int totalItems;
            using (SqlCommand countCommand = new SqlCommand(
                "SELECT COUNT(*) FROM (" + feedSource + ") AS DashboardFeed;", connection))
            {
                totalItems = Convert.ToInt32(countCommand.ExecuteScalar());
            }

            int pageCount = Math.Max(1, (int)Math.Ceiling(totalItems / (double)FeedItemsPerPage));
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, pageCount));

            using (SqlCommand command = new SqlCommand(
                "SELECT FeedType,TenderId,ActivityDate,Area,Details,Actor,CurrentStatus" +
                " FROM (" + feedSource + @") AS DashboardFeed
                  ORDER BY ActivityDate DESC,FeedType,SortId DESC
                  OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;", connection))
            {
                ModuleDb.AddParameter(command, "@Offset", SqlDbType.Int, (CurrentPage - 1) * FeedItemsPerPage);
                ModuleDb.AddParameter(command, "@PageSize", SqlDbType.Int, FeedItemsPerPage);
                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable data = new DataTable();
                    adapter.Fill(data);
                    DashboardFeedGrid.DataSource = data;
                    DashboardFeedGrid.DataBind();
                }
            }

            BindPageNumbers(pageCount);
            PreviousPageButton.Enabled = CurrentPage > 1;
            NextPageButton.Enabled = CurrentPage < pageCount;
            PageSummary.Text = String.Format(
                "Page {0} of {1} · {2} records · Showing {3}-{4}",
                CurrentPage,
                pageCount,
                totalItems,
                totalItems == 0 ? 0 : (CurrentPage - 1) * FeedItemsPerPage + 1,
                Math.Min(CurrentPage * FeedItemsPerPage, totalItems));
        }

        private void BindPageNumbers(int pageCount)
        {
            DataTable pages = new DataTable();
            pages.Columns.Add("PageNumber", typeof(int));
            pages.Columns.Add("IsCurrent", typeof(bool));
            int firstPage = Math.Max(1, Math.Min(CurrentPage - 3, pageCount - 6));
            int lastPage = Math.Min(pageCount, firstPage + 6);
            for (int page = firstPage; page <= lastPage; page++)
            {
                pages.Rows.Add(page, page == CurrentPage);
            }
            PageNumbers.DataSource = pages;
            PageNumbers.DataBind();
        }

        protected int FeedSerialNumber(int rowIndex)
        {
            return (CurrentPage - 1) * FeedItemsPerPage + rowIndex + 1;
        }

        protected void PreviousPageButton_Click(object sender, EventArgs e)
        {
            CurrentPage--;
            BindCurrentFeedPage();
        }

        protected void NextPageButton_Click(object sender, EventArgs e)
        {
            CurrentPage++;
            BindCurrentFeedPage();
        }

        protected void PageNumbers_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int requestedPage;
            if (Int32.TryParse(Convert.ToString(e.CommandArgument), out requestedPage) && requestedPage > 0)
            {
                CurrentPage = requestedPage;
                BindCurrentFeedPage();
            }
        }

        private void BindCurrentFeedPage()
        {
            Message.Text = String.Empty;
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    BindDashboardFeed(connection);
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Admin dashboard feed could not be loaded.", ex);
                Message.Text = "<div class=\"message\">Dashboard records could not be loaded. Please retry or contact support.</div>";
            }
        }

    }
}
