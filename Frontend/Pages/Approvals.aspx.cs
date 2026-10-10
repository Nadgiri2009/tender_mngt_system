using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class Approvals : ModulePage
    {
        protected Literal RoleName;
        protected Literal Message;
        protected GridView ApprovalGrid;

        protected void Page_Load(object sender, EventArgs e)
        {
            RoleName.Text = ModuleDb.Html(CurrentRole);
            if (!IsPostBack) BindApprovals();
        }

        private void BindApprovals()
        {
            string[] statuses = WorkflowRules.PendingStatusesForRole(CurrentRole);
            if (statuses.Length == 0)
            {
                ApprovalGrid.DataSource = new DataTable();
                ApprovalGrid.DataBind();
                return;
            }

            string[] parameters = new string[statuses.Length];
            for (int i = 0; i < statuses.Length; i++) parameters[i] = "@Status" + i;
            string sql = @"SELECT t.TenderId,t.TenderNumber,t.Title,t.CurrentStatus,t.EstimatedAmount,d.DepartmentName,t.CreatedDate
                           FROM dbo.Tender t INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                           WHERE t.IsDeleted=0 AND t.CurrentStatus IN (" + String.Join(",", parameters) + @")
                           ORDER BY t.CreatedDate DESC;";
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    for (int i = 0; i < statuses.Length; i++)
                        ModuleDb.AddParameter(command, parameters[i], SqlDbType.NVarChar, statuses[i], 50);
                    DataTable table = new DataTable();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command)) adapter.Fill(table);
                    ApprovalGrid.DataSource = table;
                    ApprovalGrid.DataBind();
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Pending approvals could not be loaded.", ex);
                Message.Text = "<div class=\"message\">Pending approvals could not be loaded. Please retry or contact support.</div>";
            }
        }

        protected void ApprovalGrid_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            ApprovalGrid.PageIndex = e.NewPageIndex;
            BindApprovals();
        }
    }
}
