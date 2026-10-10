using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class DepartmentDashboard : ModulePage
    {
        protected Literal Message;
        protected Literal TotalTenders;
        protected Literal PublishedTenders;
        protected Literal PendingCount;
        protected Literal CompletedTenders;
        protected HyperLink NewTenderLink;
        protected GridView PendingGrid;
        protected GridView RecentGrid;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                NewTenderLink.Visible = HasAnyRole("DepartmentOfficer", "Admin");
                LoadDashboard();
            }
        }

        private void LoadDashboard()
        {
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT COUNT(*) AS TotalTenders,
                                 SUM(CASE WHEN CurrentStatus = N'Tender Published' THEN 1 ELSE 0 END) AS PublishedTenders,
                                 SUM(CASE WHEN CurrentStatus LIKE N'%Pending%' OR CurrentStatus LIKE N'%Evaluation%' OR CurrentStatus LIKE N'%Opinion%' THEN 1 ELSE 0 END) AS PendingCount,
                                 SUM(CASE WHEN CurrentStatus = N'Payment Completed' THEN 1 ELSE 0 END) AS CompletedTenders
                          FROM dbo.Tender WHERE IsDeleted = 0;", connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            TotalTenders.Text = Convert.ToString(reader["TotalTenders"]);
                            PublishedTenders.Text = Convert.ToString(reader["PublishedTenders"] == DBNull.Value ? 0 : reader["PublishedTenders"]);
                            PendingCount.Text = Convert.ToString(reader["PendingCount"] == DBNull.Value ? 0 : reader["PendingCount"]);
                            CompletedTenders.Text = Convert.ToString(reader["CompletedTenders"] == DBNull.Value ? 0 : reader["CompletedTenders"]);
                        }
                    }

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

                    using (SqlCommand command = new SqlCommand(
                        @"SELECT TOP (5) t.TenderId, t.TenderNumber, t.Title, d.DepartmentName, t.CurrentStatus, t.CreatedDate
                          FROM dbo.Tender t INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                          WHERE t.IsDeleted=0 ORDER BY t.CreatedDate DESC;", connection))
                    {
                        RecentGrid.DataSource = ReadTable(command);
                        RecentGrid.DataBind();
                    }
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Could not load the department dashboard.", ex);
                Message.Text = "<div class=\"message\">Dashboard data could not be loaded. Please retry or contact support.</div>";
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
