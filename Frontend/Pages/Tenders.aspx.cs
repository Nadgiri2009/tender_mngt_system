using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class Tenders : ModulePage
    {
        protected HyperLink NewTenderLink;
        protected Literal Message;
        protected TextBox SearchText;
        protected DropDownList StatusFilter;
        protected DropDownList DepartmentFilter;
        protected Button FilterButton;
        protected GridView TenderGrid;
        protected Literal PageSummary;

        private string SortBy
        {
            get { return ViewState["SortBy"] as string ?? "CreatedDate"; }
            set { ViewState["SortBy"] = value; }
        }

        private bool SortDescending
        {
            get { return ViewState["SortDescending"] == null || (bool)ViewState["SortDescending"]; }
            set { ViewState["SortDescending"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            NewTenderLink.Visible = HasAnyRole("DepartmentOfficer", "Admin");
            if (!IsPostBack)
            {
                LoadFilters();
                BindTenders();
            }
        }

        private void LoadFilters()
        {
            StatusFilter.Items.Clear();
            StatusFilter.Items.Add(new ListItem("All statuses", ""));
            foreach (string status in WorkflowRules.Statuses) StatusFilter.Items.Add(new ListItem(status, status));

            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand("SELECT DepartmentId, DepartmentName FROM dbo.Departments WHERE IsActive=1 ORDER BY DepartmentName;", connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                DepartmentFilter.Items.Clear();
                DepartmentFilter.Items.Add(new ListItem("All departments", ""));
                while (reader.Read()) DepartmentFilter.Items.Add(new ListItem(reader.GetString(1), reader.GetInt32(0).ToString(CultureInfo.InvariantCulture)));
            }
        }

        private void BindTenders()
        {
            string orderColumn = SortBy == "Title" ? "t.Title" : SortBy == "Amount" ? "t.EstimatedAmount" : SortBy == "TenderNumber" ? "t.TenderNumber" : "t.CreatedDate";
            string orderDirection = SortDescending ? "DESC" : "ASC";
            int page = TenderGrid.PageIndex + 1;
            string filter = @" FROM dbo.Tender t INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                               WHERE t.IsDeleted=0 AND (@Search=N'' OR t.Title LIKE @SearchPattern OR t.TenderNumber LIKE @SearchPattern)
                               AND (@Status=N'' OR t.CurrentStatus=@Status)
                               AND (@DepartmentId=0 OR t.DepartmentId=@DepartmentId)";
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    int count;
                    using (SqlCommand countCommand = new SqlCommand("SELECT COUNT(*) " + filter, connection))
                    {
                        AddFilterParameters(countCommand);
                        count = Convert.ToInt32(countCommand.ExecuteScalar());
                    }
                    int pages = Math.Max(1, (int)Math.Ceiling(count / 10.0));
                    if (page > pages) { TenderGrid.PageIndex = pages - 1; page = pages; }
                    using (SqlCommand listCommand = new SqlCommand(
                        "SELECT t.TenderId,t.TenderNumber,t.Title,d.DepartmentName,t.EstimatedAmount,t.CurrentStatus,t.CreatedDate " + filter +
                        " ORDER BY " + orderColumn + " " + orderDirection + " OFFSET @Offset ROWS FETCH NEXT 10 ROWS ONLY;", connection))
                    {
                        AddFilterParameters(listCommand);
                        ModuleDb.AddParameter(listCommand, "@Offset", SqlDbType.Int, (page - 1) * 10);
                        using (SqlDataAdapter adapter = new SqlDataAdapter(listCommand))
                        {
                            DataTable table = new DataTable();
                            adapter.Fill(table);
                            TenderGrid.DataSource = table;
                            TenderGrid.DataBind();
                        }
                    }
                    PageSummary.Text = String.Format(CultureInfo.CurrentCulture, "Page {0} of {1} · {2} tenders total", page, pages, count);
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Could not retrieve tenders.", ex);
                Message.Text = "<div class=\"message\">Tender records could not be loaded. Please retry or contact support.</div>";
            }
        }

        private void AddFilterParameters(SqlCommand command)
        {
            string search = SearchText.Text == null ? String.Empty : SearchText.Text.Trim();
            AddText(command, "@Search", SqlDbType.NVarChar, search, 100);
            AddText(command, "@SearchPattern", SqlDbType.NVarChar, "%" + search + "%", 102);
            AddText(command, "@Status", SqlDbType.NVarChar, StatusFilter.SelectedValue, 50);
            int departmentId;
            ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                Int32.TryParse(DepartmentFilter.SelectedValue, out departmentId) ? departmentId : 0);
        }

        protected void FilterButton_Click(object sender, EventArgs e)
        {
            TenderGrid.PageIndex = 0;
            BindTenders();
        }

        protected void TenderGrid_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            TenderGrid.PageIndex = e.NewPageIndex;
            BindTenders();
        }

        protected void TenderGrid_Sorting(object sender, GridViewSortEventArgs e)
        {
            string key = e.SortExpression;
            if (key != "TenderNumber" && key != "Title" && key != "Amount" && key != "CreatedDate") key = "CreatedDate";
            SortDescending = SortBy == key ? !SortDescending : false;
            SortBy = key;
            TenderGrid.PageIndex = 0;
            BindTenders();
        }
    }
}
