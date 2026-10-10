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

        protected override void OnInit(EventArgs e)
        {
            string userKey = ConfigurationManager.AppSettings["SessionUserIdKey"] ?? "UserId";
            string roleKey = ConfigurationManager.AppSettings["SessionRoleKey"] ?? "RoleName";
            string departmentKey = ConfigurationManager.AppSettings["SessionDepartmentIdKey"] ?? "DepartmentId";
            int userId;

            if (Session == null || !Int32.TryParse(Convert.ToString(Session[userKey]), out userId) ||
                userId <= 0 || String.IsNullOrWhiteSpace(Convert.ToString(Session[roleKey])))
            {
                string loginUrl = ConfigurationManager.AppSettings["DepartmentPanelLoginUrl"] ?? "~/Pages/Error.aspx?code=401";
                Response.Redirect(ResolveUrl(loginUrl), true);
                return;
            }

            CurrentUserId = userId;
            CurrentRole = Convert.ToString(Session[roleKey]);
            ViewStateUserKey = Session.SessionID;
            int departmentId;
            if (Int32.TryParse(Convert.ToString(Session[departmentKey]), out departmentId) && departmentId > 0)
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
