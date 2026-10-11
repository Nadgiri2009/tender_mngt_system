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
        protected Literal Message, TotalTenders, DraftTenders, Departments, TotalUsers;
        protected GridView StatusGrid, RecentTendersGrid;

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
                        @"SELECT
                            (SELECT COUNT(*) FROM dbo.Tender WHERE IsDeleted=0) AS TotalTenders,
                            (SELECT COUNT(*) FROM dbo.Tender WHERE IsDeleted=0 AND CurrentStatus=N'Draft') AS DraftTenders,
                            (SELECT COUNT(*) FROM dbo.Departments) AS Departments,
                            (SELECT COUNT(*) FROM dbo.Users) AS TotalUsers;", connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            TotalTenders.Text = Convert.ToString(reader["TotalTenders"]);
                            DraftTenders.Text = Convert.ToString(reader["DraftTenders"]);
                            Departments.Text = Convert.ToString(reader["Departments"]);
                            TotalUsers.Text = Convert.ToString(reader["TotalUsers"]);
                        }
                    }

                    Bind(connection, StatusGrid,
                        @"SELECT CurrentStatus,COUNT(*) AS StatusCount FROM dbo.Tender
                          WHERE IsDeleted=0 GROUP BY CurrentStatus ORDER BY StatusCount DESC,CurrentStatus;");
                    Bind(connection, RecentTendersGrid,
                        @"SELECT TOP (10) t.TenderId,t.TenderNumber,t.Title,d.DepartmentName,t.CurrentStatus,t.CreatedDate
                          FROM dbo.Tender t INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                          WHERE t.IsDeleted=0 ORDER BY t.CreatedDate DESC;");
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Admin dashboard data could not be loaded.", ex);
                Message.Text = "<div class=\"message\">Dashboard data could not be loaded. Please retry or contact support.</div>";
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
