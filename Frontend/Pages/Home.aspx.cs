using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class Home : Page
    {
        protected Literal Message, ActiveTenders, PublishedTenders, RegisteredContractors, DepartmentCount;
        protected GridView LatestTendersGrid, DepartmentsGrid, AnnouncementsGrid;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) LoadPortalData();
        }

        private void LoadPortalData()
        {
            try
            {
                {
                    using (SqlConnection connection = ModuleDb.OpenConnection())
                    {
                        using (SqlCommand command = new SqlCommand(
                            @"SELECT
                                (SELECT COUNT(*) FROM dbo.Tender WHERE IsDeleted=0 AND CurrentStatus IN
                                    (N'Tender Published',N'Technical Bid Opening',N'Technical Evaluation',N'Financial Bid Opening',N'CAFO Rate Opinion',N'Financial Evaluation',N'MCA Opinion')) AS ActiveTenders,
                                (SELECT COUNT(*) FROM dbo.Tender WHERE IsDeleted=0 AND CurrentStatus<>N'Draft') AS PublishedTenders,
                                (SELECT COUNT(*) FROM dbo.Contractors) AS RegisteredContractors,
                                (SELECT COUNT(*) FROM dbo.Departments) AS DepartmentCount;", connection))
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ActiveTenders.Text = Convert.ToString(reader["ActiveTenders"]);
                                PublishedTenders.Text = Convert.ToString(reader["PublishedTenders"]);
                                RegisteredContractors.Text = Convert.ToString(reader["RegisteredContractors"]);
                                DepartmentCount.Text = Convert.ToString(reader["DepartmentCount"]);
                            }
                        }

                        Bind(connection, LatestTendersGrid,
                            @"SELECT TOP (6) t.TenderNumber,t.Title,d.DepartmentName,t.SubmissionDeadline
                              FROM dbo.Tender t INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                              WHERE t.IsDeleted=0 AND t.CurrentStatus NOT IN (N'Draft',N'Rejected')
                              ORDER BY t.CreatedDate DESC;");
                        Bind(connection, DepartmentsGrid,
                            "SELECT TOP (8) DepartmentName,DepartmentCode FROM dbo.Departments WHERE IsActive=1 ORDER BY DepartmentName;");
                        Bind(connection, AnnouncementsGrid,
                            @"SELECT TOP (8) Title,Body,PublishDate,ExpiryDate FROM dbo.Announcements
                              WHERE IsDeleted=0 AND IsPublished=1
                                AND (PublishDate IS NULL OR PublishDate<=SYSUTCDATETIME())
                                AND (ExpiryDate IS NULL OR ExpiryDate>=SYSUTCDATETIME())
                              ORDER BY COALESCE(PublishDate,CreatedDate) DESC;");
                    }
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Public portal content could not be loaded.", ex);
                Message.Text = "<div class=\"message\">Portal information could not be loaded. Please retry or contact support.</div>";
            }
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
