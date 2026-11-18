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
        protected string CurrentRoleCode { get; private set; }
        protected string CurrentAccountType { get; private set; }
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

            CurrentUserId = CurrentUserId > 0 &&
                (CurrentRoleId > 0 || CurrentAccountType == "ExternalBidder") ? CurrentUserId : 0;
            ViewStateUserKey = Session == null ? null : Session.SessionID;
            base.OnInit(e);
        }

        private void LoadIdentityAndPermissions(int userId)
        {
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(
                @"SELECT u.UserId,u.RoleId,r.RoleCode,r.RoleName,u.AccountType,u.DepartmentId,u.FullName
                  FROM dbo.Users u
                  LEFT JOIN dbo.Roles r ON r.RoleId=u.RoleId
                  WHERE u.UserId=@UserId AND u.IsActive=1
                    AND ((u.AccountType=N'Internal' AND r.IsActive=1)
                         OR (u.AccountType=N'ExternalBidder' AND u.RoleId IS NULL));", connection))
            {
                ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, userId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return;
                    CurrentUserId = Convert.ToInt32(reader["UserId"]);
                    CurrentRoleId = reader["RoleId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["RoleId"]);
                    CurrentRoleCode = reader["RoleCode"] == DBNull.Value ? String.Empty : Convert.ToString(reader["RoleCode"]);
                    CurrentAccountType = Convert.ToString(reader["AccountType"]);
                    CurrentRole = reader["RoleName"] == DBNull.Value ? "ExternalBidder" : Convert.ToString(reader["RoleName"]);
                    CurrentDepartmentId = reader["DepartmentId"] == DBNull.Value
                        ? (int?)null
                        : Convert.ToInt32(reader["DepartmentId"]);
                    Session["FullName"] = Convert.ToString(reader["FullName"]);
                }
            }

            if (CurrentRoleId <= 0)
            {
                LoadExternalPermissions();
                return;
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

        private void LoadExternalPermissions()
        {
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(
                @"SELECT p.PermissionKey
                  FROM dbo.ExternalAccountTypePermissions x
                  INNER JOIN dbo.Permissions p ON p.PermissionId=x.PermissionId AND p.IsActive=1
                  WHERE x.AccountType=@AccountType;", connection))
            {
                ModuleDb.AddParameter(command, "@AccountType", SqlDbType.NVarChar, CurrentAccountType, 40);
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
                string requestedRole = role.Trim();
                if (String.Equals(requestedRole, "Junior Engineer (JE)", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(requestedRole, "DepartmentOfficer", StringComparison.OrdinalIgnoreCase))
                    if (CurrentRoleCode == "Junior Engineer (JE)") return true;

                if (String.Equals(requestedRole, "Tender Cell", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(requestedRole, "TenderSection", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(requestedRole, "Nivida Kaksh (Tender Cell)", StringComparison.OrdinalIgnoreCase))
                    if (CurrentRoleCode == "TENDER_CELL") return true;

                if (String.Equals(requestedRole, "Tender Committee Chairman (Nivida Samiti Adhyaksha)", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(requestedRole, "TenderCommittee", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(requestedRole, "Nivida Samiti Adhyaksha", StringComparison.OrdinalIgnoreCase))
                    if (CurrentRoleCode == "CHAIRMAN") return true;

                if (String.Equals(requestedRole, "Director Auditor", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(requestedRole, "Chief Auditor", StringComparison.OrdinalIgnoreCase))
                    if (CurrentRoleCode == "DIRECTOR_AUDITOR") return true;

                if ((String.Equals(requestedRole, "Contractor", StringComparison.OrdinalIgnoreCase) ||
                     String.Equals(requestedRole, "Contractor / Vendor", StringComparison.OrdinalIgnoreCase) ||
                     String.Equals(requestedRole, "ExternalBidder", StringComparison.OrdinalIgnoreCase)) &&
                    CurrentAccountType == "ExternalBidder")
                    return true;
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
