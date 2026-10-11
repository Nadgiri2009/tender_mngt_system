using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class Login : Page
    {
        protected Literal Message;
        protected TextBox EmailText, PasswordText;
        protected Button SignInButton;

        protected void PasswordLength_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = args.Value != null && args.Value.Length >= 6;
        }

        protected void SignInButton_Click(object sender, EventArgs e)
        {
            Page.Validate("Login");
            if (!Page.IsValid) return;

            try
            {
                int userId;
                int? departmentId;
                string fullName;
                string role;

                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT u.UserId,u.FullName,u.PasswordHash,u.DepartmentId,r.RoleName
                          FROM dbo.Users u INNER JOIN dbo.Roles r ON r.RoleId=u.RoleId
                          WHERE u.Email=@Email AND u.IsActive=1 AND r.IsActive=1;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@Email", SqlDbType.NVarChar, EmailText.Text.Trim(), 150);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read() || !BCrypt.Net.BCrypt.Verify(PasswordText.Text, Convert.ToString(reader["PasswordHash"])))
                            {
                                Message.Text = "<div class=\"message\">Invalid email or password.</div>";
                                return;
                            }

                            userId = Convert.ToInt32(reader["UserId"]);
                            fullName = Convert.ToString(reader["FullName"]);
                            role = Convert.ToString(reader["RoleName"]);
                            departmentId = reader["DepartmentId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["DepartmentId"]);
                        }
                    }

                    ModuleDb.AddAudit(connection, transaction, userId, "Login", "User", userId, null, Request.UserHostAddress);
                    transaction.Commit();
                }

                string userKey = ConfigurationManager.AppSettings["SessionUserIdKey"] ?? "UserId";
                string roleKey = ConfigurationManager.AppSettings["SessionRoleKey"] ?? "RoleName";
                string departmentKey = ConfigurationManager.AppSettings["SessionDepartmentIdKey"] ?? "DepartmentId";
                Session[userKey] = userId;
                Session[roleKey] = role;
                Session[departmentKey] = departmentId.HasValue ? (object)departmentId.Value : null;
                Session["FullName"] = fullName;

                string destination = String.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
                    ? "~/Pages/Admin.aspx"
                    : "~/Pages/Default.aspx";
                Response.Redirect(ResolveUrl(destination), false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Sign-in could not be completed.", ex);
                Message.Text = "<div class=\"message\">Sign-in is temporarily unavailable. Please retry or contact support.</div>";
            }
        }
    }
}
