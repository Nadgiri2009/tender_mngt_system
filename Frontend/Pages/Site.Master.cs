using System;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace DepartmentModule.Pages
{
    public partial class SiteMaster : MasterPage
    {
        protected HyperLink DashboardLink, ApprovalsLink, AdminLink, LoginLink, RegisterLink;
        protected Literal CurrentUser;
        protected LinkButton SignOutLink;

        protected void Page_Load(object sender, EventArgs e)
        {
            string userKey = ConfigurationManager.AppSettings["SessionUserIdKey"] ?? "UserId";
            string roleKey = ConfigurationManager.AppSettings["SessionRoleKey"] ?? "RoleName";
            bool signedIn = Session != null && Int32.TryParse(Convert.ToString(Session[userKey]), out int userId)
                && userId > 0 && !String.IsNullOrWhiteSpace(Convert.ToString(Session[roleKey]));
            string role = signedIn ? Convert.ToString(Session[roleKey]) : String.Empty;

            DashboardLink.Visible = signedIn;
            ApprovalsLink.Visible = signedIn;
            AdminLink.Visible = signedIn && String.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
            LoginLink.Visible = !signedIn;
            RegisterLink.Visible = !signedIn;
            SignOutLink.Visible = signedIn;
            CurrentUser.Text = signedIn
                ? "<span class=\"nav-user\">" + HttpUtility.HtmlEncode(Convert.ToString(Session["FullName"])) + "</span>"
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
