using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class AdminAudit : ModulePage
    {
        private const int PageSize = 25;
        protected Literal Message, PageSummary;
        protected TextBox SearchText, EntityText, FromDateText, ToDateText;
        protected Button FilterButton, PreviousButton, NextButton;
        protected GridView AuditGrid;

        private int CurrentPage
        {
            get { return ViewState["AuditPage"] == null ? 0 : (int)ViewState["AuditPage"]; }
            set { ViewState["AuditPage"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequireRoles("Admin")) return;
            if (!IsPostBack) BindAudit();
        }

        protected void FilterButton_Click(object sender, EventArgs e)
        {
            DateTime? fromDate, toDate;
            if (!TryReadDateFilters(out fromDate, out toDate)) return;
            CurrentPage = 0;
            BindAudit();
        }

        protected void PreviousButton_Click(object sender, EventArgs e)
        {
            if (CurrentPage > 0) CurrentPage--;
            BindAudit();
        }

        protected void NextButton_Click(object sender, EventArgs e)
        {
            CurrentPage++;
            BindAudit();
        }

        private void BindAudit()
        {
            DateTime? fromDate, toDate;
            if (!TryReadDateFilters(out fromDate, out toDate)) return;

            const string fromSql = @" FROM dbo.AuditLog a
                LEFT JOIN dbo.Users u ON u.UserId=a.UserId
                WHERE (@Search=N'' OR a.Action LIKE @SearchPattern OR a.EntityName LIKE @SearchPattern
                       OR COALESCE(u.FullName,N'') LIKE @SearchPattern
                       OR COALESCE(a.Details,N'') LIKE @SearchPattern
                       OR CONVERT(NVARCHAR(20),a.EntityId) LIKE @SearchPattern)
                  AND (@Entity=N'' OR a.EntityName LIKE @EntityPattern)
                  AND (@FromDate IS NULL OR a.CreatedDate>=@FromDate)
                  AND (@ToDate IS NULL OR a.CreatedDate<@ToDate)";

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    int rowCount;
                    using (SqlCommand countCommand = new SqlCommand("SELECT COUNT(*)" + fromSql, connection))
                    {
                        AddFilterParameters(countCommand, fromDate, toDate);
                        rowCount = Convert.ToInt32(countCommand.ExecuteScalar());
                    }

                    int pageCount = Math.Max(1, (int)Math.Ceiling(rowCount / (double)PageSize));
                    if (CurrentPage >= pageCount) CurrentPage = pageCount - 1;

                    using (SqlCommand command = new SqlCommand(
                        @"SELECT a.CreatedDate,COALESCE(NULLIF(u.FullName,N''),N'System / unknown') AS Actor,
                                 a.Action,a.EntityName,a.EntityId,a.Details,a.IPAddress" + fromSql +
                        " ORDER BY a.CreatedDate DESC,a.LogId DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;", connection))
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        AddFilterParameters(command, fromDate, toDate);
                        ModuleDb.AddParameter(command, "@Offset", SqlDbType.Int, CurrentPage * PageSize);
                        ModuleDb.AddParameter(command, "@PageSize", SqlDbType.Int, PageSize);
                        DataTable data = new DataTable();
                        adapter.Fill(data);
                        AuditGrid.DataSource = data;
                        AuditGrid.DataBind();
                    }

                    PageSummary.Text = String.Format(CultureInfo.CurrentCulture,
                        "Page {0} of {1} · {2} audit entries", CurrentPage + 1, pageCount, rowCount);
                    PreviousButton.Enabled = CurrentPage > 0;
                    NextButton.Enabled = CurrentPage + 1 < pageCount;
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Audit log data could not be loaded.", ex);
                ShowMessage("Audit logs could not be loaded. Please retry or contact support.");
            }
        }

        private void AddFilterParameters(SqlCommand command, DateTime? fromDate, DateTime? toDate)
        {
            string search = (SearchText.Text ?? String.Empty).Trim();
            string entity = (EntityText.Text ?? String.Empty).Trim();
            ModuleDb.AddParameter(command, "@Search", SqlDbType.NVarChar, search, 100);
            ModuleDb.AddParameter(command, "@SearchPattern", SqlDbType.NVarChar, "%" + search + "%", 102);
            ModuleDb.AddParameter(command, "@Entity", SqlDbType.NVarChar, entity, 100);
            ModuleDb.AddParameter(command, "@EntityPattern", SqlDbType.NVarChar, "%" + entity + "%", 102);
            ModuleDb.AddParameter(command, "@FromDate", SqlDbType.DateTime2,
                fromDate.HasValue ? (object)fromDate.Value : null);
            ModuleDb.AddParameter(command, "@ToDate", SqlDbType.DateTime2,
                toDate.HasValue ? (object)toDate.Value.AddDays(1) : null);
        }

        private bool TryReadDateFilters(out DateTime? fromDate, out DateTime? toDate)
        {
            fromDate = null;
            toDate = null;
            DateTime parsed;
            if (!String.IsNullOrWhiteSpace(FromDateText.Text))
            {
                if (!DateTime.TryParseExact(FromDateText.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out parsed))
                {
                    ShowMessage("Enter a valid start date.");
                    return false;
                }
                fromDate = parsed.Date;
            }
            if (!String.IsNullOrWhiteSpace(ToDateText.Text))
            {
                if (!DateTime.TryParseExact(ToDateText.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out parsed))
                {
                    ShowMessage("Enter a valid end date.");
                    return false;
                }
                if (parsed.Date == DateTime.MaxValue.Date)
                {
                    ShowMessage("The end date is outside the supported range.");
                    return false;
                }
                toDate = parsed.Date;
            }
            if (fromDate.HasValue && toDate.HasValue && toDate.Value < fromDate.Value)
            {
                ShowMessage("The end date cannot be earlier than the start date.");
                return false;
            }
            return true;
        }

        private void ShowMessage(string text)
        {
            Message.Text = "<div class=\"message\">" + System.Web.HttpUtility.HtmlEncode(text) + "</div>";
        }
    }
}
