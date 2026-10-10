using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace DepartmentModule.Pages
{
    public partial class ModuleError : Page
    {
        protected Literal Heading;
        protected Literal Message;

        protected void Page_Load(object sender, EventArgs e)
        {
            bool forbidden = Request.QueryString["code"] == "403";
            bool unauthenticated = Request.QueryString["code"] == "401";
            Heading.Text = unauthenticated ? "Department sign-in required" : forbidden ? "Access denied" : "Something went wrong";
            Message.Text = unauthenticated
                ? "Sign in through the main Department Panel, then open this module from its navigation. This module does not provide a separate login."
                : forbidden
                    ? "Your account does not have permission to access this module page."
                    : "The request could not be completed. The error has been logged. Please retry or contact support.";
        }
    }
}
