using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class AdminReports : ModulePage
    {
        protected Literal Message, ResultSummary;
        protected DropDownList ReportTypeSelect, DepartmentSelect, StatusSelect;
        protected TextBox FromDateText, ToDateText, SearchText;
        protected Button RunReportButton, ExportButton;
        protected GridView ReportGrid;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequireRoles("Admin")) return;
            if (!IsPostBack)
            {
                try
                {
                    LoadDepartments();
                    LoadStatusOptions();
                }
                catch (SqlException ex)
                {
                    Trace.Warn("DepartmentModule", "Report filters could not be loaded.", ex);
                    ShowMessage("Report filters could not be loaded. Please retry or contact support.");
                    RunReportButton.Enabled = false;
                    ExportButton.Enabled = false;
                }
            }
        }

        protected void ReportTypeSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadStatusOptions();
            ReportGrid.PageIndex = 0;
            ResultSummary.Text = String.Empty;
        }

        protected void RunReportButton_Click(object sender, EventArgs e)
        {
            ReportGrid.PageIndex = 0;
            BindReport();
        }

        protected void ReportGrid_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            ReportGrid.PageIndex = e.NewPageIndex;
            BindReport();
        }

        protected void ExportButton_Click(object sender, EventArgs e)
        {
            DataTable data;
            if (!TryGetReportData(out data)) return;
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    ModuleDb.AddAudit(connection, transaction, CurrentUserId, "Export", "Report", null,
                        "CSV report exported: " + ReportTypeSelect.SelectedValue, Request.UserHostAddress);
                    transaction.Commit();
                }

                Response.Clear();
                Response.Buffer = true;
                Response.ContentType = "text/csv";
                Response.ContentEncoding = Encoding.UTF8;
                Response.AddHeader("Content-Disposition", "attachment; filename=\"tender-management-report.csv\"");
                Response.Write('\uFEFF');
                WriteCsv(data);
                Response.Flush();
                Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Report export could not be audited or completed.", ex);
                ShowMessage("The report could not be exported. Please retry or contact support.");
            }
        }

        private void LoadDepartments()
        {
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(
                "SELECT DepartmentId,DepartmentName FROM dbo.Departments ORDER BY DepartmentName;", connection))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                DepartmentSelect.Items.Clear();
                DepartmentSelect.Items.Add(new ListItem("All departments", ""));
                while (reader.Read())
                    DepartmentSelect.Items.Add(new ListItem(Convert.ToString(reader["DepartmentName"]),
                        Convert.ToString(reader["DepartmentId"], CultureInfo.InvariantCulture)));
            }
        }

        private void LoadStatusOptions()
        {
            StatusSelect.Items.Clear();
            StatusSelect.Items.Add(new ListItem("All statuses", ""));
            if (ReportTypeSelect.SelectedValue == "tenders" || ReportTypeSelect.SelectedValue == "deadlines")
            {
                foreach (string status in WorkflowRules.Statuses)
                    StatusSelect.Items.Add(new ListItem(status, status));
            }
            else
            {
                StatusSelect.Items.Add(new ListItem("Active", "Active"));
                StatusSelect.Items.Add(new ListItem("Inactive", "Inactive"));
            }
        }

        private void BindReport()
        {
            DataTable data;
            if (!TryGetReportData(out data)) return;
            ReportGrid.DataSource = data;
            ReportGrid.DataBind();
            ResultSummary.Text = String.Format(CultureInfo.CurrentCulture, "{0:N0} records", data.Rows.Count);
        }

        private bool TryGetReportData(out DataTable data)
        {
            data = null;
            DateTime? fromDate, toDate;
            if (!TryReadDateFilters(out fromDate, out toDate)) return false;

            string departmentValue = DepartmentSelect.SelectedValue;
            int departmentId;
            if (!String.IsNullOrEmpty(departmentValue) &&
                (!Int32.TryParse(departmentValue, NumberStyles.None, CultureInfo.InvariantCulture, out departmentId) || departmentId <= 0))
            {
                ShowMessage("Select a valid department.");
                return false;
            }

            string sql = GetReportSql();
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(sql, connection))
                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    AddReportParameters(command, fromDate, toDate);
                    data = new DataTable();
                    adapter.Fill(data);
                }
                return true;
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Requested report could not be loaded.", ex);
                ShowMessage("The report could not be loaded. Please retry or contact support.");
                return false;
            }
        }

        private string GetReportSql()
        {
            switch (ReportTypeSelect.SelectedValue)
            {
                case "contractors":
                    return @"SELECT c.ContractorId,c.Name,c.RegistrationNo,c.ContactNo,c.Email,
                                 u.FullName AS LinkedUser,
                                 CASE WHEN c.IsActive=1 THEN N'Active' ELSE N'Inactive' END AS AccountStatus,
                                 c.CreatedDate
                             FROM dbo.Contractors c
                             LEFT JOIN dbo.Users u ON u.UserId=c.UserId
                             WHERE (@FromDate IS NULL OR c.CreatedDate>=@FromDate)
                               AND (@ToDate IS NULL OR c.CreatedDate<@ToDate)
                               AND (@Status=N'' OR (@Status=N'Active' AND c.IsActive=1)
                                    OR (@Status=N'Inactive' AND c.IsActive=0))
                               AND (@Search=N'' OR c.Name LIKE @SearchPattern OR c.RegistrationNo LIKE @SearchPattern
                                    OR COALESCE(c.ContactNo,N'') LIKE @SearchPattern OR COALESCE(c.Email,N'') LIKE @SearchPattern)
                             ORDER BY c.CreatedDate DESC,c.ContractorId DESC;";

                case "users":
                    return @"SELECT u.UserId,u.FullName,u.Email,u.Mobile,r.RoleName,d.DepartmentName,
                                 CASE WHEN u.IsActive=1 THEN N'Active' ELSE N'Inactive' END AS AccountStatus,
                                 u.CreatedDate,lastLogin.LastSuccessfulLogin
                             FROM dbo.Users u
                             INNER JOIN dbo.Roles r ON r.RoleId=u.RoleId
                             LEFT JOIN dbo.Departments d ON d.DepartmentId=u.DepartmentId
                             OUTER APPLY (
                                 SELECT MAX(a.CreatedDate) AS LastSuccessfulLogin
                                 FROM dbo.AuditLog a
                                 WHERE a.UserId=u.UserId AND a.Action=N'Login' AND a.EntityName=N'User'
                             ) lastLogin
                             WHERE (@FromDate IS NULL OR u.CreatedDate>=@FromDate)
                               AND (@ToDate IS NULL OR u.CreatedDate<@ToDate)
                               AND (@DepartmentId=0 OR u.DepartmentId=@DepartmentId)
                               AND (@Status=N'' OR (@Status=N'Active' AND u.IsActive=1)
                                    OR (@Status=N'Inactive' AND u.IsActive=0))
                               AND (@Search=N'' OR u.FullName LIKE @SearchPattern OR u.Email LIKE @SearchPattern
                                    OR COALESCE(u.Mobile,N'') LIKE @SearchPattern)
                             ORDER BY u.CreatedDate DESC,u.UserId DESC;";

                case "deadlines":
                    return @"SELECT t.TenderId,t.TenderNumber,t.Title,d.DepartmentName,t.CurrentStatus,
                                 t.SubmissionDeadline,t.CreatedDate
                             FROM dbo.Tender t
                             INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                             WHERE t.IsDeleted=0 AND t.SubmissionDeadline>=SYSUTCDATETIME()
                               AND (@FromDate IS NULL OR t.SubmissionDeadline>=@FromDate)
                               AND (@ToDate IS NULL OR t.SubmissionDeadline<@ToDate)
                               AND (@DepartmentId=0 OR t.DepartmentId=@DepartmentId)
                               AND (@Status=N'' OR t.CurrentStatus=@Status)
                               AND (@Search=N'' OR t.Title LIKE @SearchPattern OR t.TenderNumber LIKE @SearchPattern)
                             ORDER BY t.SubmissionDeadline,t.TenderId;";

                default:
                    return @"SELECT t.TenderId,t.TenderNumber,t.Title,d.DepartmentName,t.CurrentStatus,
                                 t.EstimatedAmount,t.SubmissionDeadline,t.CreatedDate,t.ModifiedDate
                             FROM dbo.Tender t
                             INNER JOIN dbo.Departments d ON d.DepartmentId=t.DepartmentId
                             WHERE t.IsDeleted=0 AND (@FromDate IS NULL OR t.CreatedDate>=@FromDate)
                               AND (@ToDate IS NULL OR t.CreatedDate<@ToDate)
                               AND (@DepartmentId=0 OR t.DepartmentId=@DepartmentId)
                               AND (@Status=N'' OR t.CurrentStatus=@Status)
                               AND (@Search=N'' OR t.Title LIKE @SearchPattern OR t.TenderNumber LIKE @SearchPattern)
                             ORDER BY t.CreatedDate DESC,t.TenderId DESC;";
            }
        }

        private void AddReportParameters(SqlCommand command, DateTime? fromDate, DateTime? toDate)
        {
            string search = (SearchText.Text ?? String.Empty).Trim();
            int departmentId;
            ModuleDb.AddParameter(command, "@FromDate", SqlDbType.DateTime2,
                fromDate.HasValue ? (object)fromDate.Value : null);
            ModuleDb.AddParameter(command, "@ToDate", SqlDbType.DateTime2,
                toDate.HasValue ? (object)toDate.Value.AddDays(1) : null);
            ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                Int32.TryParse(DepartmentSelect.SelectedValue, NumberStyles.None, CultureInfo.InvariantCulture, out departmentId)
                    ? departmentId : 0);
            ModuleDb.AddParameter(command, "@Status", SqlDbType.NVarChar, StatusSelect.SelectedValue, 50);
            ModuleDb.AddParameter(command, "@Search", SqlDbType.NVarChar, search, 100);
            ModuleDb.AddParameter(command, "@SearchPattern", SqlDbType.NVarChar, "%" + search + "%", 102);
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

        private static void WriteCsv(DataTable data)
        {
            for (int i = 0; i < data.Columns.Count; i++)
            {
                if (i > 0) HttpContext.Current.Response.Write(",");
                HttpContext.Current.Response.Write(CsvField(data.Columns[i].ColumnName));
            }
            HttpContext.Current.Response.Write("\r\n");

            foreach (DataRow row in data.Rows)
            {
                for (int i = 0; i < data.Columns.Count; i++)
                {
                    if (i > 0) HttpContext.Current.Response.Write(",");
                    HttpContext.Current.Response.Write(CsvField(Convert.ToString(row[i], CultureInfo.InvariantCulture)));
                }
                HttpContext.Current.Response.Write("\r\n");
            }
        }

        private static string CsvField(string value)
        {
            string safeValue = value ?? String.Empty;
            string trimmed = safeValue.TrimStart();
            if (trimmed.StartsWith("=", StringComparison.Ordinal) || trimmed.StartsWith("+", StringComparison.Ordinal) ||
                trimmed.StartsWith("-", StringComparison.Ordinal) || trimmed.StartsWith("@", StringComparison.Ordinal))
                safeValue = "'" + safeValue;
            return "\"" + safeValue.Replace("\"", "\"\"") + "\"";
        }

        private void ShowMessage(string text)
        {
            Message.Text = "<div class=\"message\">" + HttpUtility.HtmlEncode(text) + "</div>";
        }
    }
}
