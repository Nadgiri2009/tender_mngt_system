using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class Home : Page
    {
        private const int TendersPerPage = 10;
        private const string PageIndexKey = "HomeTenderPageIndex";
        private const string PublicTenderFilter =
            " FROM dbo.Tender t INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId " +
            "WHERE t.IsDeleted=0 AND EXISTS " +
            "(SELECT 1 FROM dbo.ApprovalHistory ah WHERE ah.TenderId=t.TenderId AND ah.NewStatus=N'Tender Published')";

        protected Literal Message, ActiveTenders, PublishedTenders, RegisteredContractors, PageSummary;
        protected GridView TendersGrid, AnnouncementsGrid;
        protected LinkButton PreviousPageButton, NextPageButton;
        protected Repeater PageNumbers;

        private int CurrentPage
        {
            get { return ViewState[PageIndexKey] is int ? (int)ViewState[PageIndexKey] : 1; }
            set { ViewState[PageIndexKey] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) LoadPortalData();
        }

        private void LoadPortalData()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT
                            (SELECT COUNT(*) FROM dbo.Tender WHERE IsDeleted=0 AND CurrentStatus IN
                                (N'Tender Published',N'Technical Bid Opening',N'Technical Evaluation',N'Financial Bid Opening',N'CAFO Rate Opinion',N'Financial Evaluation',N'MCA Opinion')) AS ActiveTenders,
                            (SELECT COUNT(*) FROM dbo.Tender t
                             WHERE t.IsDeleted=0 AND EXISTS
                                (SELECT 1 FROM dbo.ApprovalHistory ah
                                 WHERE ah.TenderId=t.TenderId AND ah.NewStatus=N'Tender Published')) AS PublishedTenders,
                            (SELECT COUNT(*) FROM dbo.Contractors) AS RegisteredContractors;", connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ActiveTenders.Text = Convert.ToString(reader["ActiveTenders"]);
                            PublishedTenders.Text = Convert.ToString(reader["PublishedTenders"]);
                            RegisteredContractors.Text = Convert.ToString(reader["RegisteredContractors"]);
                        }
                    }

                    BindTenders(connection);
                    Bind(connection, AnnouncementsGrid,
                        @"SELECT TOP (8) Title,Body,PublishDate,ExpiryDate FROM dbo.Announcements
                          WHERE IsDeleted=0 AND IsPublished=1
                            AND (PublishDate IS NULL OR PublishDate<=SYSUTCDATETIME())
                            AND (ExpiryDate IS NULL OR ExpiryDate>=SYSUTCDATETIME())
                          ORDER BY COALESCE(PublishDate,CreatedDate) DESC;");
                }
            }
            catch (SqlException ex)
            {
                ShowLoadError("Public portal content could not be loaded.", ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                ShowLoadError("Public portal database configuration is invalid.", ex);
            }
        }

        private void BindTenders(SqlConnection connection)
        {
            int count;
            using (SqlCommand countCommand = new SqlCommand("SELECT COUNT(*)" + PublicTenderFilter + ";", connection))
            {
                count = Convert.ToInt32(countCommand.ExecuteScalar());
            }

            int pageCount = Math.Max(1, (int)Math.Ceiling(count / (double)TendersPerPage));
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, pageCount));
            using (SqlCommand command = new SqlCommand(
                @"SELECT t.TenderId,t.TenderNumber,t.Title,t.TenderType,d.DepartmentName,
                         (SELECT MIN(ah.ApprovalDate)
                          FROM dbo.ApprovalHistory ah
                          WHERE ah.TenderId=t.TenderId AND ah.NewStatus=N'Tender Published') AS PublicationDate,
                         t.SubmissionStartDate,t.SubmissionDeadline,t.CurrentStatus" + PublicTenderFilter + @"
                  ORDER BY t.CreatedDate DESC,t.TenderId DESC
                  OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;", connection))
            {
                ModuleDb.AddParameter(command, "@Offset", SqlDbType.Int, (CurrentPage - 1) * TendersPerPage);
                ModuleDb.AddParameter(command, "@PageSize", SqlDbType.Int, TendersPerPage);
                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable tenders = new DataTable();
                    adapter.Fill(tenders);
                    TendersGrid.DataSource = tenders;
                    TendersGrid.DataBind();
                }
            }

            BindPageNumbers(pageCount);
            PreviousPageButton.Enabled = CurrentPage > 1;
            NextPageButton.Enabled = CurrentPage < pageCount;
            PageSummary.Text = String.Format(
                "Page {0} of {1} · {2} tenders total · Showing {3}-{4}",
                CurrentPage,
                pageCount,
                count,
                count == 0 ? 0 : (CurrentPage - 1) * TendersPerPage + 1,
                Math.Min(CurrentPage * TendersPerPage, count));
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

        protected int TenderSerialNumber(int rowIndex)
        {
            return (CurrentPage - 1) * TendersPerPage + rowIndex + 1;
        }

        protected void PreviousPageButton_Click(object sender, EventArgs e)
        {
            CurrentPage--;
            BindCurrentPage();
        }

        protected void NextPageButton_Click(object sender, EventArgs e)
        {
            CurrentPage++;
            BindCurrentPage();
        }

        protected void PageNumbers_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int requestedPage;
            if (Int32.TryParse(Convert.ToString(e.CommandArgument), out requestedPage) && requestedPage > 0)
            {
                CurrentPage = requestedPage;
                BindCurrentPage();
            }
        }

        private void BindCurrentPage()
        {
            Message.Text = String.Empty;
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    BindTenders(connection);
                }
            }
            catch (SqlException ex)
            {
                ShowLoadError("Tender records could not be loaded.", ex);
            }
            catch (ConfigurationErrorsException ex)
            {
                ShowLoadError("Tender database configuration is invalid.", ex);
            }
        }

        private void ShowLoadError(string message, Exception exception)
        {
            Trace.Warn("DepartmentModule", message, exception);
            Message.Text = "<div class=\"message\">" + message + " Please retry or contact support.</div>";
            TendersGrid.DataSource = null;
            TendersGrid.DataBind();
            PageNumbers.DataSource = null;
            PageNumbers.DataBind();
            PreviousPageButton.Enabled = false;
            NextPageButton.Enabled = false;
            PageSummary.Text = "Tender list unavailable.";
        }

        private static void Bind(SqlConnection connection, GridView grid, string sql)
        {
            using (SqlCommand command = new SqlCommand(sql, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                DataTable data = new DataTable();
                adapter.Fill(data);
                grid.DataSource = data;
                grid.DataBind();
            }
        }
    }
}
