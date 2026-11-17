using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace DepartmentModule.Helpers
{
    public static class TenderLifecycleFiles
    {
        private static readonly string[] AllowedExtensions =
            { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
        public static bool IsAllowedTenderDocumentType(string documentType)
        {
            if (String.IsNullOrWhiteSpace(documentType)) return false;
            using (SqlConnection connection = ModuleDb.OpenConnection())
            using (SqlCommand command = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.DocumentTypes WHERE TypeName=@TypeName AND IsActive=1;", connection))
            {
                ModuleDb.AddParameter(command, "@TypeName", SqlDbType.NVarChar, documentType.Trim(), 80);
                return Convert.ToInt32(command.ExecuteScalar()) == 1;
            }
        }

        public static void ConvertDatesToIndia(DataTable data, params string[] columns)
        {
            TimeZoneInfo indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            foreach (string column in columns)
            {
                if (!data.Columns.Contains(column)) continue;
                foreach (DataRow row in data.Rows)
                {
                    if (row[column] != DBNull.Value)
                    {
                        row[column] = TimeZoneInfo.ConvertTimeFromUtc(
                            DateTime.SpecifyKind(Convert.ToDateTime(row[column], CultureInfo.InvariantCulture), DateTimeKind.Utc),
                            indiaTimeZone);
                    }
                }
            }
        }

        public static DateTime ParseIndiaDateTime(string date, string time, string label)
        {
            DateTime local;
            if (!DateTime.TryParseExact(
                    (date ?? String.Empty).Trim() + " " + (time ?? String.Empty).Trim(),
                    "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out local))
            {
                throw new InvalidOperationException("Enter a valid " + label + " date and time.");
            }

            TimeZoneInfo indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), indiaTimeZone);
        }

        public static string FormatIndiaDateTime(object utcValue)
        {
            if (utcValue == null || utcValue == DBNull.Value) return String.Empty;
            TimeZoneInfo indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            DateTime utc = DateTime.SpecifyKind(Convert.ToDateTime(utcValue, CultureInfo.InvariantCulture), DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, indiaTimeZone).ToString("dd MMM yyyy HH:mm", CultureInfo.CurrentCulture);
        }

        public static string SaveUpload(Page page, FileUpload upload, int tenderId, string uploadKind)
        {
            return SaveUpload(page, upload, tenderId, uploadKind, "tender-lifecycle");
        }

        public static string SaveTenderBidUpload(Page page, FileUpload upload, int tenderId)
        {
            return SaveUpload(page, upload, tenderId, "bid", "tender-bids");
        }

        private static string SaveUpload(Page page, FileUpload upload, int tenderId, string uploadKind,
            string relativeFolderName)
        {
            if (upload == null || !upload.HasFile)
                throw new InvalidOperationException("Select the required " + uploadKind + " document.");

            string extension = Path.GetExtension(upload.FileName).ToLowerInvariant();
            if (upload.PostedFile.ContentLength <= 0 ||
                upload.PostedFile.ContentLength > 20 * 1024 * 1024 ||
                Array.IndexOf(AllowedExtensions, extension) < 0 ||
                !HasAllowedContentSignature(upload.PostedFile.InputStream, extension))
            {
                throw new InvalidOperationException("Choose a valid PDF, Office document, JPEG or PNG no larger than 20 MB.");
            }

            string storedName = Guid.NewGuid().ToString("N") + extension;
            string relativeFolder = relativeFolderName + "/" + tenderId.ToString(CultureInfo.InvariantCulture);
            string uploadRoot = System.Configuration.ConfigurationManager.AppSettings["UploadRoot"] ?? "~/Uploads";
            string folder = page.Server.MapPath(uploadRoot.TrimEnd('/', '\\') + "/" + relativeFolder);
            Directory.CreateDirectory(folder);
            upload.SaveAs(Path.Combine(folder, storedName));
            return "Uploads/" + relativeFolder + "/" + storedName;
        }

        public static void DeleteStoredUpload(Page page, string relativePath)
        {
            DeleteStoredUpload(page, relativePath, "tender-lifecycle");
        }

        public static void DeleteStoredBidUpload(Page page, string relativePath)
        {
            DeleteStoredUpload(page, relativePath, "tender-bids");
        }

        private static void DeleteStoredUpload(Page page, string relativePath, string relativeFolderName)
        {
            if (String.IsNullOrWhiteSpace(relativePath)) return;
            string normalized = relativePath.Replace('\\', '/').TrimStart('/');
            string expectedPrefix = "Uploads/" + relativeFolderName + "/";
            if (!normalized.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("..") || Path.IsPathRooted(normalized)) return;

            string uploadRoot = System.Configuration.ConfigurationManager.AppSettings["UploadRoot"] ?? "~/Uploads";
            string root = Path.GetFullPath(page.Server.MapPath(uploadRoot));
            string path = Path.GetFullPath(Path.Combine(
                root, normalized.Substring("Uploads/".Length).Replace('/', Path.DirectorySeparatorChar)));
            if (path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase) && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static bool HasAllowedContentSignature(Stream stream, string extension)
        {
            byte[] header = new byte[8];
            long originalPosition = stream.CanSeek ? stream.Position : 0;
            int count = stream.Read(header, 0, header.Length);
            if (stream.CanSeek) stream.Position = originalPosition;

            if (extension == ".pdf")
                return count >= 5 && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 &&
                    header[3] == 0x46 && header[4] == 0x2D;
            if (extension == ".jpg" || extension == ".jpeg")
                return count >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            if (extension == ".png")
                return count == 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E &&
                    header[3] == 0x47 && header[4] == 0x0D && header[5] == 0x0A &&
                    header[6] == 0x1A && header[7] == 0x0A;
            if (extension == ".doc" || extension == ".xls")
                return count >= 8 && header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 &&
                    header[3] == 0xE0 && header[4] == 0xA1 && header[5] == 0xB1 &&
                    header[6] == 0x1A && header[7] == 0xE1;
            return (extension == ".docx" || extension == ".xlsx") && count >= 4 &&
                header[0] == 0x50 && header[1] == 0x4B &&
                (header[2] == 0x03 || header[2] == 0x05 || header[2] == 0x07) &&
                (header[3] == 0x04 || header[3] == 0x06 || header[3] == 0x08);
        }

        public static string SafeFileName(string fileName)
        {
            return Path.GetFileName(fileName ?? String.Empty);
        }
    }
}
