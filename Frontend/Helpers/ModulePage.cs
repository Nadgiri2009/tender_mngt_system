using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;

namespace DepartmentModule.Helpers
{
    public class ModulePage : Page
    {
        protected int CurrentUserId { get; private set; }
        protected int CurrentRoleId { get; private set; }
        protected string CurrentRole { get; private set; }
        protected int? CurrentDepartmentId { get; private set; }
        private readonly HashSet<string> CurrentPermissions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        protected virtual bool RequiresAuthentication
        {
            get { return true; }
        }

        protected override void OnInit(EventArgs e)
        {
            string userKey = ConfigurationManager.AppSettings["SessionUserIdKey"] ?? "UserId";
            int userId = 0;
            bool hasSessionIdentity = Session != null &&
                Int32.TryParse(Convert.ToString(Session[userKey]), out userId) && userId > 0;

            if (!hasSessionIdentity && RequiresAuthentication)
            {
                string loginUrl = ConfigurationManager.AppSettings["DepartmentPanelLoginUrl"] ?? "~/Pages/Error.aspx?code=401";
                Response.Redirect(ResolveUrl(loginUrl), true);
                return;
            }

            if (hasSessionIdentity)
            {
                LoadIdentityAndPermissions(userId);
                if (CurrentUserId == 0)
                {
                    Session.Clear();
                    Session.Abandon();
                    if (RequiresAuthentication)
                    {
                        string loginUrl = ConfigurationManager.AppSettings["DepartmentPanelLoginUrl"] ?? "~/Pages/Error.aspx?code=401";
                        Response.Redirect(ResolveUrl(loginUrl), true);
                        return;
                    }
                }
            }

            CurrentUserId = CurrentRoleId > 0 ? CurrentUserId : 0;
            ViewStateUserKey = Session == null ? null : Session.SessionID;
            base.OnInit(e);
        }

        private void LoadIdentityAndPermissions(int userId)
        {
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(
                @"SELECT u.UserId,u.RoleId,r.RoleName,u.DepartmentId,u.FullName
                  FROM dbo.Users u
                  INNER JOIN dbo.Roles r ON r.RoleId=u.RoleId
                  WHERE u.UserId=@UserId AND u.IsActive=1 AND r.IsActive=1;", connection))
            {
                ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, userId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return;
                    CurrentUserId = Convert.ToInt32(reader["UserId"]);
                    CurrentRoleId = Convert.ToInt32(reader["RoleId"]);
                    CurrentRole = Convert.ToString(reader["RoleName"]);
                    CurrentDepartmentId = reader["DepartmentId"] == DBNull.Value
                        ? (int?)null
                        : Convert.ToInt32(reader["DepartmentId"]);
                    Session["FullName"] = Convert.ToString(reader["FullName"]);
                }
            }

            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(
                @"SELECT p.PermissionKey
                  FROM dbo.RolePermissions rp
                  INNER JOIN dbo.Permissions p ON p.PermissionId=rp.PermissionId
                  WHERE rp.RoleId=@RoleId AND p.IsActive=1;", connection))
            {
                ModuleDb.AddParameter(command, "@RoleId", SqlDbType.Int, CurrentRoleId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read()) CurrentPermissions.Add(Convert.ToString(reader["PermissionKey"]));
                }
            }
        }

        protected bool HasAnyRole(params string[] roles)
        {
            foreach (string role in roles)
            {
                if (String.Equals(CurrentRole, role, StringComparison.OrdinalIgnoreCase) &&
                    HasPermission("legacy.role." + role.ToLowerInvariant()))
                {
                    return true;
                }

                if (String.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) &&
                    String.Equals(CurrentRole, "Super Admin", StringComparison.OrdinalIgnoreCase) &&
                    HasPermission("system.admin")) return true;
                if (String.Equals(role, "Contractor", StringComparison.OrdinalIgnoreCase) &&
                    String.Equals(CurrentRole, "Contractor / Vendor", StringComparison.OrdinalIgnoreCase) &&
                    HasPermission("bid.submit.own")) return true;
            }
            return false;
        }

        protected bool HasPermission(string permissionKey)
        {
            return !String.IsNullOrWhiteSpace(permissionKey) && CurrentPermissions.Contains(permissionKey);
        }

        protected bool RequirePermission(string permissionKey)
        {
            if (HasPermission(permissionKey)) return true;
            Response.StatusCode = 403;
            Response.TrySkipIisCustomErrors = true;
            Response.Redirect("~/Pages/Error.aspx?code=403", true);
            return false;
        }

        protected bool RequireAnyPermission(params string[] permissionKeys)
        {
            foreach (string permissionKey in permissionKeys)
            {
                if (HasPermission(permissionKey)) return true;
            }
            Response.StatusCode = 403;
            Response.TrySkipIisCustomErrors = true;
            Response.Redirect("~/Pages/Error.aspx?code=403", true);
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

        protected bool WorkflowRolesAllowed(params string[] roles)
        {
            foreach (string role in roles)
            {
                if (HasAnyRole(role)) return true;
            }
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
