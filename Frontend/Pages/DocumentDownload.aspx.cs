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
        protected override bool RequiresAuthentication
        {
            get { return false; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            int extensionRequestId = QueryId("extensionRequestId");
            if (extensionRequestId > 0)
            {
                string kind = Convert.ToString(Request.QueryString["kind"]);
                if (kind != "application" && kind != "supporting") { NotFound(); return; }
                DownloadExtensionDocument(extensionRequestId, kind);
                return;
            }

            int evaluationDocumentId = QueryId("evaluationDocumentId");
            if (evaluationDocumentId > 0)
            {
                DownloadEvaluationDocument(evaluationDocumentId);
                return;
            }

            int technicalBidId = QueryId("technicalBidId");
            if (technicalBidId > 0)
            {
                DownloadTechnicalBidDocument(technicalBidId);
                return;
            }

            int documentId = QueryId("id");
            if (documentId <= 0) { NotFound(); return; }
            string storedPath;
            string fileName;
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                    @"SELECT td.FilePath,td.FileName
                      FROM dbo.TenderDocuments td
                      INNER JOIN dbo.Tender t ON t.TenderId=td.TenderId
                      WHERE td.DocumentId=@DocumentId AND t.IsDeleted=0
                        AND (
                            ((@CurrentUserId=0 OR @CurrentRole=N'Contractor') AND EXISTS
                                (SELECT 1 FROM dbo.ApprovalHistory publicHistory
                                 WHERE publicHistory.TenderId=t.TenderId AND publicHistory.NewStatus=N'Tender Published'))
                            OR (@CurrentUserId>0 AND @CurrentRole<>N'Contractor'
                                AND (@CurrentRole<>N'DepartmentOfficer' OR t.CreatedBy=@CurrentUserId))
                        )
                        AND (@CurrentUserId=0 OR @CurrentRole=N'Contractor' OR @CurrentRole=N'Admin'
                             OR NOT EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                           WHERE scopeUser.UserId=@CurrentUserId
                                             AND scopeUser.DepartmentId IS NOT NULL
                                             AND scopeUser.DepartmentId<>t.DepartmentId));", connection))
                {
                    ModuleDb.AddParameter(command, "@DocumentId", SqlDbType.Int, documentId);
                    ModuleDb.AddParameter(command, "@CurrentUserId", SqlDbType.Int, CurrentUserId);
                    ModuleDb.AddParameter(command, "@CurrentRole", SqlDbType.NVarChar, CurrentRole, 50);
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

            SendStoredFile(storedPath, fileName);
        }

        private void DownloadExtensionDocument(int requestId, string kind)
        {
            if (!HasAnyRole("DepartmentOfficer", "CAFOOfficer", "Admin")) { NotFound(); return; }
            string storedPath, fileName;
            bool isAdmin = HasAnyRole("Admin");
            string departmentScope = isAdmin ? String.Empty :
                @" AND (@DepartmentId IS NULL OR t.DepartmentId=@DepartmentId)
                   AND (NOT EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                   WHERE scopeUser.UserId=@ScopeUserId AND scopeUser.DepartmentId IS NOT NULL)
                        OR EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                  WHERE scopeUser.UserId=@ScopeUserId AND scopeUser.DepartmentId=t.DepartmentId))";
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                    @"SELECT CASE WHEN @Kind=N'application' THEN e.ApplicationFilePath ELSE e.SupportingFilePath END AS FilePath,
                             CASE WHEN @Kind=N'application' THEN e.ApplicationFileName ELSE e.SupportingFileName END AS FileName
                      FROM dbo.TenderExtensionRequests e
                      INNER JOIN dbo.Tender t ON t.TenderId=e.TenderId
                      WHERE e.ExtensionRequestId=@RequestId AND t.IsDeleted=0 AND
                        (@Role=N'Admin'
                         OR (@Role=N'DepartmentOfficer' AND e.SubmittedBy=@UserId)
                         OR @Role=N'CAFOOfficer')" + departmentScope + ";", connection))
                {
                    ModuleDb.AddParameter(command, "@Kind", SqlDbType.NVarChar, kind, 12);
                    ModuleDb.AddParameter(command, "@RequestId", SqlDbType.Int, requestId);
                    ModuleDb.AddParameter(command, "@Role", SqlDbType.NVarChar, CurrentRole, 50);
                    ModuleDb.AddParameter(command, "@UserId", SqlDbType.Int, CurrentUserId);
                    if (!isAdmin)
                    {
                        ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                            CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                        ModuleDb.AddParameter(command, "@ScopeUserId", SqlDbType.Int, CurrentUserId);
                    }
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read()) { NotFound(); return; }
                        storedPath = Convert.ToString(reader["FilePath"]);
                        fileName = TenderLifecycleFiles.SafeFileName(Convert.ToString(reader["FileName"]));
                    }
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Extension document metadata could not be read.", ex);
                Response.StatusCode = 500;
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            SendStoredFile(storedPath, fileName);
        }

        private void DownloadEvaluationDocument(int documentId)
        {
            if (!HasAnyRole("TenderSection", "TenderCommittee", "Admin")) { NotFound(); return; }
            string storedPath, fileName;
            bool isAdmin = HasAnyRole("Admin");
            string departmentScope = isAdmin ? String.Empty :
                @" AND (@DepartmentId IS NULL OR t.DepartmentId=@DepartmentId)
                   AND (NOT EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                   WHERE scopeUser.UserId=@ScopeUserId AND scopeUser.DepartmentId IS NOT NULL)
                        OR EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                  WHERE scopeUser.UserId=@ScopeUserId AND scopeUser.DepartmentId=t.DepartmentId))";
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                    @"SELECT d.FilePath,d.FileName
                      FROM dbo.TenderEvaluationDocuments d
                      INNER JOIN dbo.Tender t ON t.TenderId=d.TenderId
                      WHERE d.EvaluationDocumentId=@DocumentId AND t.IsDeleted=0
                        AND EXISTS(SELECT 1 FROM dbo.TenderBidOpening o WHERE o.TenderId=t.TenderId)" +
                    departmentScope + ";", connection))
                {
                    ModuleDb.AddParameter(command, "@DocumentId", SqlDbType.Int, documentId);
                    if (!isAdmin)
                    {
                        ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                            CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                        ModuleDb.AddParameter(command, "@ScopeUserId", SqlDbType.Int, CurrentUserId);
                    }
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read()) { NotFound(); return; }
                        storedPath = Convert.ToString(reader["FilePath"]);
                        fileName = TenderLifecycleFiles.SafeFileName(Convert.ToString(reader["FileName"]));
                    }
                }
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Evaluation document metadata could not be read.", ex);
                Response.StatusCode = 500;
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            SendStoredFile(storedPath, fileName);
        }

        private void DownloadTechnicalBidDocument(int technicalBidId)
        {
            if (!HasAnyRole("TenderSection", "TenderCommittee", "Admin")) { NotFound(); return; }
            string storedPath, fileName;
            bool isAdmin = HasAnyRole("Admin");
            string departmentScope = isAdmin ? String.Empty :
                @" AND (@DepartmentId IS NULL OR t.DepartmentId=@DepartmentId)
                   AND (NOT EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                   WHERE scopeUser.UserId=@ScopeUserId AND scopeUser.DepartmentId IS NOT NULL)
                        OR EXISTS(SELECT 1 FROM dbo.Users scopeUser
                                  WHERE scopeUser.UserId=@ScopeUserId AND scopeUser.DepartmentId=t.DepartmentId))";
            try
            {
                using (SqlConnection connection = ModuleDb.OpenConnection())
                using (SqlCommand command = new SqlCommand(
                    @"SELECT b.DocumentPath,b.TenderId
                      FROM dbo.TechnicalBid b
                      INNER JOIN dbo.Tender t ON t.TenderId=b.TenderId
                      WHERE b.TechnicalBidId=@BidId AND t.IsDeleted=0
                        AND EXISTS(SELECT 1 FROM dbo.TenderBidOpening o WHERE o.TenderId=t.TenderId)" +
                    departmentScope + ";", connection))
                {
                    ModuleDb.AddParameter(command, "@BidId", SqlDbType.Int, technicalBidId);
                    if (!isAdmin)
                    {
                        ModuleDb.AddParameter(command, "@DepartmentId", SqlDbType.Int,
                            CurrentDepartmentId.HasValue ? (object)CurrentDepartmentId.Value : null);
                        ModuleDb.AddParameter(command, "@ScopeUserId", SqlDbType.Int, CurrentUserId);
                    }
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read() || reader["DocumentPath"] == DBNull.Value) { NotFound(); return; }
                        storedPath = Convert.ToString(reader["DocumentPath"]);
                    }
                }
                fileName = Path.GetFileName(storedPath);
            }
            catch (SqlException ex)
            {
                Trace.Warn("DepartmentModule", "Technical bid document metadata could not be read.", ex);
                Response.StatusCode = 500;
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            SendStoredFile(storedPath, fileName);
        }

        private void SendStoredFile(string storedPath, string fileName)
        {
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
