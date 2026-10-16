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
        protected TextBox MobileText, OtpText;
        protected Panel OtpPanel;
        protected Button RequestOtpButton, VerifyOtpButton;
        protected HyperLink RegisterLink;

        private const string OtpUserIdKey = "LoginOtpUserId";
        private const string OtpMobileKey = "LoginOtpMobile";
        private const string OtpHashKey = "LoginOtpHash";
        private const string OtpSaltKey = "LoginOtpSalt";
        private const string OtpExpiresKey = "LoginOtpExpiresUtc";
        private const string OtpAttemptsKey = "LoginOtpAttempts";
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
            RegisterLink.NavigateUrl = ResolveUrl("~/Pages/Register.aspx" +
                (applyTenderId > 0 ? "?applyTenderId=" + applyTenderId.ToString() : String.Empty));
        }

        protected void Mobile_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = OtpChallenge.IsValidIndianMobile(args.Value);
        }

        protected void RequestOtpButton_Click(object sender, EventArgs e)
        {
            Page.Validate("LoginId");
            if (!Page.IsValid) return;

            ClearOtpChallenge();
            OtpPanel.Visible = false;
            string normalizedMobile = OtpChallenge.NormalizeMobile(MobileText.Text);
            try
            {
                DateTime now = DateTime.UtcNow;
                if (!OtpChallenge.TryReserveRequest(Session, "Login", now))
                {
                    Message.Text = "<div class=\"message\">You have reached the limit of 3 OTP requests in 15 minutes. Please wait before requesting another code.</div>";
                    return;
                }

                int userId = 0;
                bool ambiguousMobile = false;
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                        @"SELECT TOP (2) u.UserId
                          FROM dbo.Users u INNER JOIN dbo.Roles r ON r.RoleId=u.RoleId
                          WHERE " + OtpChallenge.IndianMobileMatchSql + @"
                            AND u.IsActive=1 AND r.IsActive=1
                          ORDER BY u.UserId;", connection))
                {
                    ModuleDb.AddParameter(command, "@Mobile", SqlDbType.NVarChar, normalizedMobile, 20);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            userId = Convert.ToInt32(reader["UserId"]);
                            ambiguousMobile = reader.Read();
                        }
                    }
                }

                if (ambiguousMobile)
                {
                    Trace.Warn("DepartmentModule", "Login OTP was not issued because multiple active accounts share a mobile number.");
                    userId = 0;
                }

                if (userId == 0)
                {
                    Message.Text = "<div class=\"message\">If an active account with a registered mobile number exists, a login OTP will be issued.</div>";
                    return;
                }

                string otp = OtpChallenge.CreateCode();
                string deliveryMode;
                if (!OtpChallenge.TryDeliver("login", otp, normalizedMobile, out deliveryMode))
                {
                    Message.Text = String.Equals(deliveryMode, "Sms", StringComparison.OrdinalIgnoreCase)
                        ? "<div class=\"message\">The SMS gateway could not send the OTP. Please retry later or contact support.</div>"
                        : "<div class=\"message\">OTP delivery is unavailable on this server. Please contact support.</div>";
                    return;
                }

                byte[] salt = OtpChallenge.CreateSalt();
                Session[OtpUserIdKey] = userId;
                Session[OtpMobileKey] = normalizedMobile;
                Session[OtpHashKey] = OtpChallenge.HashCode(otp, salt);
                Session[OtpSaltKey] = salt;
                Session[OtpExpiresKey] = now.AddMinutes(OtpLifetimeMinutes);
                Session[OtpAttemptsKey] = 0;
                OtpPanel.Visible = true;
                Message.Text = String.Equals(deliveryMode, "Sms", StringComparison.OrdinalIgnoreCase)
                    ? "<div class=\"message success\">A login OTP has been sent to your registered mobile number.</div>"
                    : String.Empty;
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Login OTP could not be issued.", ex);
                Message.Text = "<div class=\"message\">Login is temporarily unavailable. Please retry or contact support.</div>";
            }
        }

        protected void VerifyOtpButton_Click(object sender, EventArgs e)
        {
            Page.Validate("LoginId");
            if (!Page.IsValid) return;
            Page.Validate("LoginOtp");
            if (!Page.IsValid) return;

            int userId = Session[OtpUserIdKey] is int ? (int)Session[OtpUserIdKey] : 0;
            string pendingMobile = Session[OtpMobileKey] as string;
            string expectedHash = Session[OtpHashKey] as string;
            byte[] salt = Session[OtpSaltKey] as byte[];
            DateTime expiresUtc = Session[OtpExpiresKey] is DateTime
                ? (DateTime)Session[OtpExpiresKey]
                : DateTime.MinValue;

            if (userId <= 0 || expectedHash == null || salt == null ||
                !String.Equals(pendingMobile, OtpChallenge.NormalizeMobile(MobileText.Text), StringComparison.Ordinal))
            {
                ClearOtpChallenge();
                Message.Text = "<div class=\"message\">Request a new login OTP.</div>";
                OtpPanel.Visible = false;
                return;
            }

            if (DateTime.UtcNow > expiresUtc)
            {
                ClearOtpChallenge();
                Message.Text = "<div class=\"message\">The OTP has expired. Request a new one.</div>";
                OtpPanel.Visible = false;
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

            ClearOtpChallenge();
            CompleteLogin(userId, pendingMobile);
        }

        private void CompleteLogin(int userId, string verifiedMobile)
        {
            try
            {
                int? departmentId;
                string fullName;
                string role;
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT u.UserId,u.FullName,u.DepartmentId,r.RoleName
                          FROM dbo.Users u INNER JOIN dbo.Roles r ON r.RoleId=u.RoleId
                          WHERE u.UserId=@UserId AND " + OtpChallenge.IndianMobileMatchSql + @"
                            AND u.IsActive=1 AND r.IsActive=1;", connection, transaction))
                    {
                        ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, userId);
                        ModuleDb.AddParameter(command, "@Mobile", SqlDbType.NVarChar, verifiedMobile, 20);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                Message.Text = "<div class=\"message\">This account is no longer active or its mobile number changed. Request a new OTP.</div>";
                                return;
                            }
                            fullName = Convert.ToString(reader["FullName"]);
                            role = Convert.ToString(reader["RoleName"]);
                            departmentId = reader["DepartmentId"] == DBNull.Value
                                ? (int?)null
                                : Convert.ToInt32(reader["DepartmentId"]);
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

                int applyTenderId = GetApplyTenderId();
                string destination = String.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
                    ? "~/Pages/Admin.aspx"
                    : String.Equals(role, "Contractor", StringComparison.OrdinalIgnoreCase) && applyTenderId > 0
                        ? "~/Pages/Bids.aspx?tenderId=" + applyTenderId.ToString()
                        : "~/Pages/Default.aspx";
                Response.Redirect(ResolveUrl(destination), false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Sign-in could not be completed after OTP verification.", ex);
                Message.Text = "<div class=\"message\">Sign-in is temporarily unavailable. Please request a new OTP and retry.</div>";
            }
        }

        private void ClearOtpChallenge()
        {
            Session.Remove(OtpUserIdKey);
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
