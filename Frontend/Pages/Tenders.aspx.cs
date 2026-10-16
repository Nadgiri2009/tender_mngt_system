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
        protected DropDownList StatusFilter, CategoryFilter;
        protected TextBox DateFrom, DateTo;
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
            StatusFilter.Items.Add(new ListItem("Approved", "Approved"));
            StatusFilter.Items.Add(new ListItem("Pending", "Pending"));
            StatusFilter.Items.Add(new ListItem("Under Review", "Under Review"));
            StatusFilter.Items.Add(new ListItem("Rejected", "Rejected"));
            StatusFilter.Items.Add(new ListItem("Draft / Submitted", "Draft / Submitted"));
            foreach (string status in WorkflowRules.Statuses) StatusFilter.Items.Add(new ListItem(status, status));

            using (SqlConnection connection = ModuleDb.OpenConnection())
            {
                using (SqlCommand command = new SqlCommand(
                    "SELECT DISTINCT TenderType FROM dbo.Tender WHERE IsDeleted=0 ORDER BY TenderType;", connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    CategoryFilter.Items.Clear();
                    CategoryFilter.Items.Add(new ListItem("All categories", ""));
                    while (reader.Read()) CategoryFilter.Items.Add(new ListItem(reader.GetString(0), reader.GetString(0)));
                }

                using (SqlCommand command = new SqlCommand("SELECT DepartmentId, DepartmentName FROM dbo.Departments WHERE IsActive=1 ORDER BY DepartmentName;", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        DepartmentFilter.Items.Clear();
                        DepartmentFilter.Items.Add(new ListItem("All departments", ""));
                        while (reader.Read())
                            DepartmentFilter.Items.Add(new ListItem(reader.GetString(1), reader.GetInt32(0).ToString(CultureInfo.InvariantCulture)));
                    }
                }
            }

            string requestedBucket = Request.QueryString["bucket"];
            string requestedStatus = Request.QueryString["status"];
            if (!String.IsNullOrWhiteSpace(requestedBucket) && StatusFilter.Items.FindByValue(requestedBucket) != null)
                StatusFilter.SelectedValue = requestedBucket;
            else if (!String.IsNullOrWhiteSpace(requestedStatus) && StatusFilter.Items.FindByValue(requestedStatus) != null)
                StatusFilter.SelectedValue = requestedStatus;
        }

        private void BindTenders()
        {
            string orderColumn = SortBy == "Title" ? "t.Title" : SortBy == "Amount" ? "t.EstimatedAmount" : SortBy == "TenderNumber" ? "t.TenderNumber" : "t.CreatedDate";
            string orderDirection = SortDescending ? "DESC" : "ASC";
            int page = TenderGrid.PageIndex + 1;
            string ownerFilter = HasAnyRole("DepartmentOfficer") ? " AND t.CreatedBy=@CurrentUserId" : String.Empty;
            string statusBucket = TenderStatusBuckets.Predicate(StatusFilter.SelectedValue);
            object dateFrom;
            object dateTo;
            if (!TryGetDateFilters(out dateFrom, out dateTo))
            {
                TenderGrid.DataSource = null;
                TenderGrid.DataBind();
                PageSummary.Text = "Date filters are invalid.";
                return;
            }
            string filter = @" FROM dbo.Tender t INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                               LEFT JOIN dbo.Users u ON u.UserId=t.CreatedBy
                               WHERE t.IsDeleted=0 AND (@Search=N'' OR t.Title LIKE @SearchPattern OR t.TenderNumber LIKE @SearchPattern)
                               AND (@Status=N'' OR t.CurrentStatus=@Status" +
                               (statusBucket == null ? String.Empty : " OR (" + statusBucket + ")") +
                               @")
                               AND (@Category=N'' OR t.TenderType=@Category)
                               AND (@DateFrom IS NULL OR t.SubmissionDeadline>=@DateFrom)
                               AND (@DateTo IS NULL OR t.SubmissionDeadline<DATEADD(DAY,1,@DateTo))
                               AND (@DepartmentId=0 OR t.DepartmentId=@DepartmentId)" + ownerFilter;
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    int count;
                    using (SqlCommand countCommand = new SqlCommand("SELECT COUNT(*) " + filter, connection))
                    {
                        AddFilterParameters(countCommand, dateFrom, dateTo);
                        AddOwnerParameter(countCommand);
                        count = Convert.ToInt32(countCommand.ExecuteScalar());
                    }
                    int pages = Math.Max(1, (int)Math.Ceiling(count / 10.0));
                    if (page > pages) { TenderGrid.PageIndex = pages - 1; page = pages; }
                    using (SqlCommand listCommand = new SqlCommand(
                        @"SELECT t.TenderId,t.TenderNumber,t.Title,t.TenderType,d.DepartmentName,
                                 COALESCE(u.FullName,N'') AS AssignedOfficer,t.EstimatedAmount,
                                 (SELECT MIN(ah.ApprovalDate) FROM dbo.ApprovalHistory ah
                                  WHERE ah.TenderId=t.TenderId AND ah.NewStatus=N'Tender Published') AS PublicationDate,
                                 t.SubmissionStartDate,t.SubmissionDeadline,t.CurrentStatus,t.CreatedDate " + filter +
                        " ORDER BY " + orderColumn + " " + orderDirection + " OFFSET @Offset ROWS FETCH NEXT 10 ROWS ONLY;", connection))
                    {
                        AddFilterParameters(listCommand, dateFrom, dateTo);
                        AddOwnerParameter(listCommand);
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

        private void AddFilterParameters(SqlCommand command, object dateFrom, object dateTo)
        {
            string search = SearchText.Text == null ? String.Empty : SearchText.Text.Trim();
            AddText(command, "@Search", SqlDbType.NVarChar, search, 100);
            AddText(command, "@SearchPattern", SqlDbType.NVarChar, "%" + search + "%", 102);
            AddText(command, "@Status", SqlDbType.NVarChar, StatusFilter.SelectedValue, 50);
            AddText(command, "@Category", SqlDbType.NVarChar, CategoryFilter.SelectedValue, 50);
            ModuleDb.AddParameter(command, "@DateFrom", SqlDbType.Date, dateFrom);
            ModuleDb.AddParameter(command, "@DateTo", SqlDbType.Date, dateTo);
            int departmentId;
            ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                Int32.TryParse(DepartmentFilter.SelectedValue, out departmentId) ? departmentId : 0);
        }

        private bool TryGetDateFilters(out object dateFrom, out object dateTo)
        {
            dateFrom = null;
            dateTo = null;
            DateTime parsed;
            if (!String.IsNullOrWhiteSpace(DateFrom.Text))
            {
                if (!DateTime.TryParseExact(DateFrom.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                {
                    Message.Text = "<div class=\"message\">Enter a valid deadline start date.</div>";
                    return false;
                }
                dateFrom = parsed.Date;
            }
            if (!String.IsNullOrWhiteSpace(DateTo.Text))
            {
                if (!DateTime.TryParseExact(DateTo.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                {
                    Message.Text = "<div class=\"message\">Enter a valid deadline end date.</div>";
                    return false;
                }
                dateTo = parsed.Date;
            }
            if (dateFrom != null && dateTo != null && (DateTime)dateFrom > (DateTime)dateTo)
            {
                Message.Text = "<div class=\"message\">The deadline start date must be on or before the end date.</div>";
                return false;
            }
            Message.Text = String.Empty;
            return true;
        }

        private void AddOwnerParameter(SqlCommand command)
        {
            if (HasAnyRole("DepartmentOfficer"))
                ModuleDb.AddParameter(command, "@CurrentUserId", SqlDbType.Int, CurrentUserId);
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

        protected string TenderStatusCssClass(object status)
        {
            return TenderStatusBuckets.CssClass(Convert.ToString(status, CultureInfo.CurrentCulture));
        }
    }
}
