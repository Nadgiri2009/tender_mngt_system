using System;
using System.Web;

namespace DepartmentModule
{
    public class ModuleApplication : HttpApplication
    {
        protected void Application_Error(object sender, EventArgs e)
        {
            Exception error = Server.GetLastError();
            if (error != null)
            {
                System.Diagnostics.Trace.TraceError("DepartmentModule: Unhandled module error. {0}", error);
            }
        }
    }
}
