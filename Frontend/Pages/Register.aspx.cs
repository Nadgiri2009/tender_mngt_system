using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class Register : Page
    {
        protected Literal Message;
        protected TextBox FullNameText, EmailText, MobileText, PasswordText;
        protected Button RegisterButton;

        protected void FullName_ServerValidate(object source, ServerValidateEventArgs args)
        {
            string name = (args.Value ?? String.Empty).Trim();
            args.IsValid = name.Length >= 2 && name.Length <= 150;
        }

        protected void Mobile_ServerValidate(object source, ServerValidateEventArgs args)
        {
            string mobile = args.Value ?? String.Empty;
            string digits = new string(mobile.Where(Char.IsDigit).ToArray());
            args.IsValid = digits.Length >= 10 && digits.Length <= 15 && Regex.IsMatch(mobile, @"^[+0-9() .-]+$");
        }

        protected void Password_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = args.Value != null && args.Value.Length >= 6;
        }

        protected void RegisterButton_Click(object sender, EventArgs e)
        {
            Page.Validate("Register");
            if (!Page.IsValid) return;

            string email = EmailText.Text.Trim();
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    int roleId;
                    using (SqlCommand roleCommand = new SqlCommand(
                        "SELECT RoleId FROM dbo.Roles WHERE RoleName=N'Contractor' AND IsActive=1;", connection, transaction))
                    {
                        object roleValue = roleCommand.ExecuteScalar();
                        if (roleValue == null || roleValue == DBNull.Value)
                        {
                            Message.Text = "<div class=\"message\">Contractor registration is not configured. Please contact support.</div>";
                            return;
                        }
                        roleId = Convert.ToInt32(roleValue);
                    }

                    using (SqlCommand duplicateCommand = new SqlCommand(
                        "SELECT COUNT(1) FROM dbo.Users WHERE Email=@Email;", connection, transaction))
                    {
                        ModuleDb.AddParameter(duplicateCommand, "@Email", SqlDbType.NVarChar, email, 150);
                        if (Convert.ToInt32(duplicateCommand.ExecuteScalar()) > 0)
                        {
                            Message.Text = "<div class=\"message\">A user with this email already exists.</div>";
                            return;
                        }
                    }

                    int userId;
                    using (SqlCommand insertCommand = new SqlCommand(
                        @"INSERT INTO dbo.Users (FullName,Email,Mobile,PasswordHash,RoleId,DepartmentId,IsActive,CreatedDate)
                          OUTPUT INSERTED.UserId
                          VALUES (@FullName,@Email,@Mobile,@PasswordHash,@RoleId,NULL,1,SYSUTCDATETIME());", connection, transaction))
                    {
                        ModuleDb.AddParameter(insertCommand, "@FullName", SqlDbType.NVarChar, FullNameText.Text.Trim(), 150);
                        ModuleDb.AddParameter(insertCommand, "@Email", SqlDbType.NVarChar, email, 150);
                        ModuleDb.AddParameter(insertCommand, "@Mobile", SqlDbType.NVarChar, MobileText.Text.Trim(), 20);
                        ModuleDb.AddParameter(insertCommand, "@PasswordHash", SqlDbType.NVarChar,
                            BCrypt.Net.BCrypt.HashPassword(PasswordText.Text), 500);
                        ModuleDb.AddParameter(insertCommand, "@RoleId", SqlDbType.Int, roleId);
                        userId = Convert.ToInt32(insertCommand.ExecuteScalar());
                    }

                    ModuleDb.AddAudit(connection, transaction, userId, "Register", "User", userId, null, Request.UserHostAddress);
                    transaction.Commit();
                    EstablishSession(userId, FullNameText.Text.Trim(), "Contractor", null);
                    Response.Redirect(ResolveUrl("~/Pages/Tenders.aspx"), false);
                    Context.ApplicationInstance.CompleteRequest();
                }
                catch (SqlException ex)
                {
                    transaction.Rollback();
                    Trace.Warn("DepartmentModule", "Vendor registration failed.", ex);
                    Message.Text = "<div class=\"message\">Registration could not be completed. Check the details and retry, or contact support.</div>";
                }
            }
        }

        private void EstablishSession(int userId, string fullName, string role, int? departmentId)
        {
            string userKey = System.Configuration.ConfigurationManager.AppSettings["SessionUserIdKey"] ?? "UserId";
            string roleKey = System.Configuration.ConfigurationManager.AppSettings["SessionRoleKey"] ?? "RoleName";
            string departmentKey = System.Configuration.ConfigurationManager.AppSettings["SessionDepartmentIdKey"] ?? "DepartmentId";
            Session[userKey] = userId;
            Session[roleKey] = role;
            Session[departmentKey] = departmentId.HasValue ? (object)departmentId.Value : null;
            Session["FullName"] = fullName;
        }
    }
}
