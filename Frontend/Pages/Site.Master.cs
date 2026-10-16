using System;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace DepartmentModule.Pages
{
    public partial class SiteMaster : MasterPage
    {
        protected HtmlGenericControl AppLayout, AdminSidebar, PublicNavigation;
        protected HyperLink AdminDashboardLink, AdminUsersLink, AdminTendersLink, AdminCreateTenderLink,
            ApprovalsLink, AdminDepartmentsLink, AdminAnnouncementsLink, AdminReportsLink, AdminAuditLink,
            DashboardLink, PublicApprovalsLink, PublicAdminLink, LoginLink, RegisterLink;
        protected Literal CurrentUser, PublicCurrentUser;
        protected LinkButton SignOutLink, PublicSignOutLink;

        protected void Page_Load(object sender, EventArgs e)
        {
            string userKey = ConfigurationManager.AppSettings["SessionUserIdKey"] ?? "UserId";
            string roleKey = ConfigurationManager.AppSettings["SessionRoleKey"] ?? "RoleName";
            bool signedIn = Session != null && Int32.TryParse(Convert.ToString(Session[userKey]), out int userId)
                && userId > 0 && !String.IsNullOrWhiteSpace(Convert.ToString(Session[roleKey]));
            string role = signedIn ? Convert.ToString(Session[roleKey]) : String.Empty;
            bool isLoginPage = String.Equals(Request.AppRelativeCurrentExecutionFilePath,
                "~/Pages/Login.aspx", StringComparison.OrdinalIgnoreCase);
            string path = Request.AppRelativeCurrentExecutionFilePath;
            bool isAdmin = String.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
            bool isAdminWorkspace = signedIn && isAdmin &&
                (path.StartsWith("~/Pages/Admin", StringComparison.OrdinalIgnoreCase) ||
                 String.Equals(path, "~/Pages/Approvals.aspx", StringComparison.OrdinalIgnoreCase) ||
                 String.Equals(path, "~/Pages/Tenders.aspx", StringComparison.OrdinalIgnoreCase) ||
                 String.Equals(path, "~/Pages/TenderForm.aspx", StringComparison.OrdinalIgnoreCase) ||
                 String.Equals(path, "~/Pages/Bids.aspx", StringComparison.OrdinalIgnoreCase) ||
                 String.Equals(path, "~/Pages/WorkManagement.aspx", StringComparison.OrdinalIgnoreCase));

            AppLayout.Attributes["class"] = isAdminWorkspace
                ? "app-shell admin-layout"
                : "app-shell public-layout";
            AdminSidebar.Visible = isAdminWorkspace;
            PublicNavigation.Visible = !isAdminWorkspace;
            AdminDashboardLink.Visible = isAdminWorkspace;
            AdminUsersLink.Visible = isAdminWorkspace;
            AdminTendersLink.Visible = isAdminWorkspace;
            AdminCreateTenderLink.Visible = isAdminWorkspace;
            ApprovalsLink.Visible = isAdminWorkspace;
            AdminDepartmentsLink.Visible = isAdminWorkspace;
            AdminAnnouncementsLink.Visible = isAdminWorkspace;
            AdminReportsLink.Visible = isAdminWorkspace;
            AdminAuditLink.Visible = isAdminWorkspace;
            DashboardLink.Visible = signedIn && !isAdminWorkspace;
            PublicApprovalsLink.Visible = signedIn && !isAdminWorkspace;
            PublicAdminLink.Visible = signedIn && isAdmin && !isAdminWorkspace;
            LoginLink.Visible = !signedIn;
            RegisterLink.Visible = !signedIn && !isLoginPage;
            SignOutLink.Visible = signedIn && isAdminWorkspace;
            PublicSignOutLink.Visible = signedIn && !isAdminWorkspace;
            SetCurrentNavigation(path);
            string currentUser = signedIn
                ? "<span class=\"nav-user\">" + HttpUtility.HtmlEncode(Convert.ToString(Session["FullName"])) + "</span>"
                : String.Empty;
            CurrentUser.Text = isAdminWorkspace ? currentUser : String.Empty;
            PublicCurrentUser.Text = isAdminWorkspace ? String.Empty : currentUser;
        }

        private void SetCurrentNavigation(string path)
        {
            bool isTenderManagement = String.Equals(path, "~/Pages/Tenders.aspx", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(path, "~/Pages/TenderDetails.aspx", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(path, "~/Pages/Bids.aspx", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(path, "~/Pages/WorkManagement.aspx", StringComparison.OrdinalIgnoreCase);

            AdminDashboardLink.CssClass = String.Equals(path, "~/Pages/Admin.aspx", StringComparison.OrdinalIgnoreCase)
                ? "is-current"
                : String.Empty;
            AdminUsersLink.CssClass = String.Equals(path, "~/Pages/AdminUsers.aspx", StringComparison.OrdinalIgnoreCase)
                ? "is-current"
                : String.Empty;
            AdminTendersLink.CssClass = isTenderManagement ? "is-current" : String.Empty;
            AdminCreateTenderLink.CssClass = String.Equals(path, "~/Pages/TenderForm.aspx", StringComparison.OrdinalIgnoreCase)
                ? "is-current"
                : String.Empty;
            ApprovalsLink.CssClass = String.Equals(path, "~/Pages/Approvals.aspx", StringComparison.OrdinalIgnoreCase)
                ? "is-current"
                : String.Empty;
            AdminDepartmentsLink.CssClass = String.Equals(path, "~/Pages/AdminDepartments.aspx", StringComparison.OrdinalIgnoreCase)
                ? "is-current"
                : String.Empty;
            AdminAnnouncementsLink.CssClass = String.Equals(path, "~/Pages/AdminAnnouncements.aspx", StringComparison.OrdinalIgnoreCase)
                ? "is-current"
                : String.Empty;
            AdminReportsLink.CssClass = String.Equals(path, "~/Pages/AdminReports.aspx", StringComparison.OrdinalIgnoreCase)
                ? "is-current"
                : String.Empty;
            AdminAuditLink.CssClass = String.Equals(path, "~/Pages/AdminAudit.aspx", StringComparison.OrdinalIgnoreCase)
                ? "is-current"
                : String.Empty;
            DashboardLink.CssClass = String.Equals(path, "~/Pages/Default.aspx", StringComparison.OrdinalIgnoreCase)
                ? "is-current"
                : String.Empty;
            PublicApprovalsLink.CssClass = String.Equals(path, "~/Pages/Approvals.aspx", StringComparison.OrdinalIgnoreCase)
                ? "is-current"
                : String.Empty;
            PublicAdminLink.CssClass = path.StartsWith("~/Pages/Admin", StringComparison.OrdinalIgnoreCase)
                ? "is-current"
                : String.Empty;
            LoginLink.CssClass = String.Equals(path, "~/Pages/Login.aspx", StringComparison.OrdinalIgnoreCase)
                ? "is-current"
                : String.Empty;
            RegisterLink.CssClass = String.Equals(path, "~/Pages/Register.aspx", StringComparison.OrdinalIgnoreCase)
                ? "is-current"
                : String.Empty;
        }

        protected void SignOutLink_Click(object sender, EventArgs e)
        {
            if (Session != null)
            {
                Session.Clear();
                Session.Abandon();
            }
            Response.Redirect(ResolveUrl("~/Pages/Home.aspx"), false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
