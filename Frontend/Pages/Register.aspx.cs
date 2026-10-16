using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class Register : Page
    {
        protected Literal Message;
        protected TextBox FullNameText, EmailText, MobileText, OtpText;
        protected Panel OtpPanel;
        protected Button RequestOtpButton, RegisterButton;
        protected HyperLink LoginLink;

        private const string OtpMobileKey = "RegisterOtpMobile";
        private const string OtpHashKey = "RegisterOtpHash";
        private const string OtpSaltKey = "RegisterOtpSalt";
        private const string OtpExpiresKey = "RegisterOtpExpiresUtc";
        private const string OtpAttemptsKey = "RegisterOtpAttempts";
        private const int OtpLifetimeMinutes = 5;
        private const int OtpMaxAttempts = 5;

        protected void Page_Load(object sender, EventArgs e)
        {
            OtpPanel.Visible = Session[OtpHashKey] != null;
            OtpText.Attributes["inputmode"] = "numeric";
            OtpText.Attributes["pattern"] = "[0-9]*";
            MobileText.Attributes["inputmode"] = "numeric";
            MobileText.Attributes["pattern"] = "[0-9]{10}";
            MobileText.Attributes["maxlength"] = "10";
            int applyTenderId = GetApplyTenderId();
            LoginLink.NavigateUrl = ResolveUrl("~/Pages/Login.aspx" +
                (applyTenderId > 0 ? "?applyTenderId=" + applyTenderId.ToString() : String.Empty));
        }

        protected void FullName_ServerValidate(object source, ServerValidateEventArgs args)
        {
            string name = (args.Value ?? String.Empty).Trim();
            args.IsValid = name.Length >= 2 && name.Length <= 150;
        }

        protected void Mobile_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = OtpChallenge.IsValidIndianMobile(args.Value);
        }

        protected void RequestOtpButton_Click(object sender, EventArgs e)
        {
            Page.Validate("Register");
            if (!Page.IsValid) return;

            ClearOtpChallenge();
            OtpPanel.Visible = false;
            string email = EmailText.Text.Trim();
            string mobile = OtpChallenge.NormalizeMobile(MobileText.Text);
            DateTime now = DateTime.UtcNow;

            if (!OtpChallenge.TryReserveRequest(Session, "Register", now))
            {
                Message.Text = "<div class=\"message\">You have reached the limit of 3 OTP requests in 15 minutes. Please wait before requesting another code.</div>";
                return;
            }

            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                {
                    if (AccountAlreadyExists(connection, email, mobile, null))
                    {
                        Message.Text = "<div class=\"message\">An account already exists with this email or mobile number.</div>";
                        return;
                    }
                }

                string code = OtpChallenge.CreateCode();
                string deliveryMode;
                if (!OtpChallenge.TryDeliver("registration", code, mobile, out deliveryMode))
                {
                    Message.Text = String.Equals(deliveryMode, "Sms", StringComparison.OrdinalIgnoreCase)
                        ? "<div class=\"message\">The SMS gateway could not send the OTP. Please retry later or contact support.</div>"
                        : "<div class=\"message\">OTP delivery is unavailable on this server. Please contact support.</div>";
                    return;
                }

                byte[] salt = OtpChallenge.CreateSalt();
                Session[OtpMobileKey] = mobile;
                Session[OtpHashKey] = OtpChallenge.HashCode(code, salt);
                Session[OtpSaltKey] = salt;
                Session[OtpExpiresKey] = now.AddMinutes(OtpLifetimeMinutes);
                Session[OtpAttemptsKey] = 0;
                OtpPanel.Visible = true;
                Message.Text = String.Equals(deliveryMode, "Sms", StringComparison.OrdinalIgnoreCase)
                    ? "<div class=\"message success\">A registration OTP has been sent to your mobile number.</div>"
                    : String.Empty;
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Registration OTP could not be issued.", ex);
                Message.Text = "<div class=\"message\">Registration is temporarily unavailable. Please retry or contact support.</div>";
            }
        }

        protected void RegisterButton_Click(object sender, EventArgs e)
        {
            Page.Validate("Register");
            Page.Validate("RegisterOtp");
            if (!Page.IsValid) return;

            string mobile = OtpChallenge.NormalizeMobile(MobileText.Text);
            string pendingMobile = Session[OtpMobileKey] as string;
            string expectedHash = Session[OtpHashKey] as string;
            byte[] salt = Session[OtpSaltKey] as byte[];
            DateTime expiresUtc = Session[OtpExpiresKey] is DateTime
                ? (DateTime)Session[OtpExpiresKey]
                : DateTime.MinValue;

            if (expectedHash == null || salt == null ||
                !String.Equals(pendingMobile, mobile, StringComparison.Ordinal))
            {
                ClearOtpChallenge();
                OtpPanel.Visible = false;
                Message.Text = "<div class=\"message\">Request a new registration OTP.</div>";
                return;
            }

            if (DateTime.UtcNow > expiresUtc)
            {
                ClearOtpChallenge();
                OtpPanel.Visible = false;
                Message.Text = "<div class=\"message\">The OTP has expired. Request a new one.</div>";
                return;
            }

            int attempts = Session[OtpAttemptsKey] is int ? (int)Session[OtpAttemptsKey] : 0;
            attempts++;
            Session[OtpAttemptsKey] = attempts;
            if (!OtpChallenge.Matches(expectedHash, OtpText.Text, salt))
            {
                if (attempts >= OtpMaxAttempts)
                {
                    ClearOtpChallenge();
                    OtpPanel.Visible = false;
                    Message.Text = "<div class=\"message\">Too many incorrect OTP attempts. Request a new OTP.</div>";
                }
                else
                {
                    OtpPanel.Visible = true;
                    Message.Text = "<div class=\"message\">Invalid OTP.</div>";
                }
                return;
            }

            CreateAccount(mobile);
        }

        private void CreateAccount(string mobile)
        {
            string email = EmailText.Text.Trim();
            string fullName = FullNameText.Text.Trim();
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    int roleId;
                    using (SqlCommand roleCommand = new SqlCommand(
                        "SELECT RoleId FROM dbo.Roles WHERE RoleName=N'Contractor' AND IsActive=1;",
                        connection, transaction))
                    {
                        object roleValue = roleCommand.ExecuteScalar();
                        if (roleValue == null || roleValue == DBNull.Value)
                        {
                            Message.Text = "<div class=\"message\">Contractor registration is not configured. Please contact support.</div>";
                            return;
                        }
                        roleId = Convert.ToInt32(roleValue);
                    }

                    if (AccountAlreadyExists(connection, email, mobile, transaction))
                    {
                        Message.Text = "<div class=\"message\">An account already exists with this email or mobile number.</div>";
                        return;
                    }

                    int userId;
                    using (SqlCommand insertCommand = new SqlCommand(
                        @"INSERT INTO dbo.Users (FullName,Email,Mobile,PasswordHash,RoleId,DepartmentId,IsActive,CreatedDate)
                          OUTPUT INSERTED.UserId
                          VALUES (@FullName,@Email,@Mobile,@PasswordHash,@RoleId,NULL,1,SYSUTCDATETIME());",
                        connection, transaction))
                    {
                        ModuleDb.AddParameter(insertCommand, "@FullName", SqlDbType.NVarChar, fullName, 150);
                        ModuleDb.AddParameter(insertCommand, "@Email", SqlDbType.NVarChar, email, 150);
                        ModuleDb.AddParameter(insertCommand, "@Mobile", SqlDbType.NVarChar, mobile, 20);
                        // The existing schema requires this field; OTP remains the only public sign-in credential.
                        ModuleDb.AddParameter(insertCommand, "@PasswordHash", SqlDbType.NVarChar,
                            BCrypt.Net.BCrypt.HashPassword(OtpChallenge.CreateRandomToken()), 500);
                        ModuleDb.AddParameter(insertCommand, "@RoleId", SqlDbType.Int, roleId);
                        userId = Convert.ToInt32(insertCommand.ExecuteScalar());
                    }

                    ModuleDb.AddAudit(connection, transaction, userId, "Register", "User", userId, null, Request.UserHostAddress);
                    transaction.Commit();
                    ClearOtpChallenge();
                    EstablishSession(userId, fullName, "Contractor", null);
                    int applyTenderId = GetApplyTenderId();
                    string destination = applyTenderId > 0
                        ? "~/Pages/Bids.aspx?tenderId=" + applyTenderId.ToString()
                        : "~/Pages/Tenders.aspx";
                    Response.Redirect(ResolveUrl(destination), false);
                    Context.ApplicationInstance.CompleteRequest();
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Vendor registration failed.", ex);
                Message.Text = "<div class=\"message\">Registration could not be completed. Check the details and retry, or contact support.</div>";
            }
        }

        private static bool AccountAlreadyExists(SqlConnection connection, string email, string mobile, SqlTransaction transaction)
        {
            string lockHint = transaction == null ? String.Empty : " WITH (UPDLOCK,HOLDLOCK)";
            using (SqlCommand command = new SqlCommand(
                @"SELECT COUNT(1)
                  FROM dbo.Users u" + lockHint + @"
                  WHERE u.Email=@Email OR " + OtpChallenge.IndianMobileMatchSql + @";",
                connection, transaction))
            {
                ModuleDb.AddParameter(command, "@Email", SqlDbType.NVarChar, email, 150);
                ModuleDb.AddParameter(command, "@Mobile", SqlDbType.NVarChar, mobile, 20);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private void EstablishSession(int userId, string fullName, string role, int? departmentId)
        {
            string userKey = ConfigurationManager.AppSettings["SessionUserIdKey"] ?? "UserId";
            string roleKey = ConfigurationManager.AppSettings["SessionRoleKey"] ?? "RoleName";
            string departmentKey = ConfigurationManager.AppSettings["SessionDepartmentIdKey"] ?? "DepartmentId";
            Session[userKey] = userId;
            Session[roleKey] = role;
            Session[departmentKey] = departmentId.HasValue ? (object)departmentId.Value : null;
            Session["FullName"] = fullName;
        }

        private void ClearOtpChallenge()
        {
            Session.Remove(OtpMobileKey);
            Session.Remove(OtpHashKey);
            Session.Remove(OtpSaltKey);
            Session.Remove(OtpExpiresKey);
            Session.Remove(OtpAttemptsKey);
        }

        private int GetApplyTenderId()
        {
            int tenderId;
            return Int32.TryParse(Request.QueryString["applyTenderId"], out tenderId) && tenderId > 0
                ? tenderId
                : 0;
        }
    }
}
