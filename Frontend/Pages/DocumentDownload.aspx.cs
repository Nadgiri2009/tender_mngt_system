using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using DepartmentModule.Helpers;

namespace DepartmentModule.Pages
{
    public partial class DocumentDownload : ModulePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            int documentId = QueryId("id");
            if (documentId <= 0) { NotFound(); return; }
            string storedPath;
            string fileName;
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                    @"SELECT FilePath,FileName FROM dbo.TenderDocuments WHERE DocumentId=@DocumentId;", connection))
                {
                    ModuleDb.AddParameter(command, "@DocumentId", SqlDbType.Int, documentId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read()) { NotFound(); return; }
                        storedPath = Convert.ToString(reader["FilePath"]);
                        fileName = Path.GetFileName(Convert.ToString(reader["FileName"]));
                    }
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Document metadata could not be read.", ex);
                Response.StatusCode = 500;
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            string normalized = (storedPath ?? String.Empty).Replace('\\', '/').TrimStart('/');
            if (normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring("uploads/".Length);
            if (String.IsNullOrWhiteSpace(normalized) || normalized.Contains("..") || Path.IsPathRooted(normalized))
            {
                NotFound();
                return;
            }

            string uploadRoot = System.Configuration.ConfigurationManager.AppSettings["UploadRoot"] ?? "~/Uploads";
            string root = Path.GetFullPath(Server.MapPath(uploadRoot));
            string path = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(path))
            {
                NotFound();
                return;
            }

            Response.Clear();
            Response.ContentType = MimeType(Path.GetExtension(fileName));
            Response.AddHeader("Content-Disposition", "attachment; filename=\"" + fileName.Replace("\"", String.Empty) + "\"");
            Response.TransmitFile(path);
            Context.ApplicationInstance.CompleteRequest();
        }

        private static string MimeType(string extension)
        {
            switch ((extension ?? String.Empty).ToLowerInvariant())
            {
                case ".pdf": return "application/pdf";
                case ".doc": return "application/msword";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls": return "application/vnd.ms-excel";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                default: return "application/octet-stream";
            }
        }

        private void NotFound()
        {
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
