using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;

namespace DepartmentModule.Helpers
{
    public class ModulePage : Page
    {
        protected int CurrentUserId { get; private set; }
        protected string CurrentRole { get; private set; }
        protected int? CurrentDepartmentId { get; private set; }

        protected virtual bool RequiresAuthentication
        {
            get { return true; }
        }

        protected override void OnInit(EventArgs e)
        {
            string userKey = ConfigurationManager.AppSettings["SessionUserIdKey"] ?? "UserId";
            string roleKey = ConfigurationManager.AppSettings["SessionRoleKey"] ?? "RoleName";
            string departmentKey = ConfigurationManager.AppSettings["SessionDepartmentIdKey"] ?? "DepartmentId";
            int userId = 0;
            bool isAuthenticated = Session != null &&
                Int32.TryParse(Convert.ToString(Session[userKey]), out userId) &&
                userId > 0 && !String.IsNullOrWhiteSpace(Convert.ToString(Session[roleKey]));

            if (!isAuthenticated && RequiresAuthentication)
            {
                string loginUrl = ConfigurationManager.AppSettings["DepartmentPanelLoginUrl"] ?? "~/Pages/Error.aspx?code=401";
                Response.Redirect(ResolveUrl(loginUrl), true);
                return;
            }

            CurrentUserId = isAuthenticated ? userId : 0;
            CurrentRole = isAuthenticated ? Convert.ToString(Session[roleKey]) : String.Empty;
            ViewStateUserKey = Session.SessionID;
            int departmentId;
            if (isAuthenticated &&
                Int32.TryParse(Convert.ToString(Session[departmentKey]), out departmentId) && departmentId > 0)
            {
                CurrentDepartmentId = departmentId;
            }

            base.OnInit(e);
        }

        protected bool HasAnyRole(params string[] roles)
        {
            foreach (string role in roles)
            {
                if (String.Equals(CurrentRole, role, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        protected bool RequireRoles(params string[] roles)
        {
            if (HasAnyRole(roles))
            {
                return true;
            }

            Response.StatusCode = 403;
            Response.TrySkipIisCustomErrors = true;
            Response.Redirect("~/Pages/Error.aspx?code=403", true);
            return false;
        }

        protected bool RequireOwnedTenderForDepartmentOfficer(int tenderId)
        {
            if (!HasAnyRole("DepartmentOfficer")) return true;

            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.Tender WHERE TenderId=@TenderId AND CreatedBy=@UserId AND IsDeleted=0;",
                connection))
            {
                ModuleDb.AddParameter(command, "@TenderId", System.Data.SqlDbType.Int, tenderId);
                ModuleDb.AddParameter(command, "@UserId", System.Data.SqlDbType.Int, CurrentUserId);
                if (Convert.ToInt32(command.ExecuteScalar()) == 1) return true;
            }

            Response.StatusCode = 403;
            Response.TrySkipIisCustomErrors = true;
            Response.Redirect("~/Pages/Error.aspx?code=403", false);
            Context.ApplicationInstance.CompleteRequest();
            return false;
        }

        protected static int QueryId(string key)
        {
            int value;
            return Int32.TryParse(HttpContext.Current.Request.QueryString[key], out value) && value > 0 ? value : 0;
        }

        protected static void AddText(SqlCommand command, string name, System.Data.SqlDbType type, string value, int size)
        {
            ModuleDb.AddParameter(command, name, type, String.IsNullOrWhiteSpace(value) ? null : value.Trim(), size);
        }
    }
}
