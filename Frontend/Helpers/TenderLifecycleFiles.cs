using System;
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
            if (upload == null || !upload.HasFile)
                throw new InvalidOperationException("Select the required " + uploadKind + " document.");

            string extension = Path.GetExtension(upload.FileName).ToLowerInvariant();
            if (upload.PostedFile.ContentLength <= 0 ||
                upload.PostedFile.ContentLength > 20 * 1024 * 1024 ||
                Array.IndexOf(AllowedExtensions, extension) < 0)
            {
                throw new InvalidOperationException("Choose a non-empty PDF, Office document, JPEG or PNG no larger than 20 MB.");
            }

            string storedName = Guid.NewGuid().ToString("N") + extension;
            string relativeFolder = "tender-lifecycle/" + tenderId.ToString(CultureInfo.InvariantCulture);
            string uploadRoot = System.Configuration.ConfigurationManager.AppSettings["UploadRoot"] ?? "~/Uploads";
            string folder = page.Server.MapPath(uploadRoot.TrimEnd('/', '\\') + "/" + relativeFolder);
            Directory.CreateDirectory(folder);
            upload.SaveAs(Path.Combine(folder, storedName));
            return "Uploads/" + relativeFolder + "/" + storedName;
        }

        public static void DeleteStoredUpload(Page page, string relativePath)
        {
            if (String.IsNullOrWhiteSpace(relativePath)) return;
            string normalized = relativePath.Replace('\\', '/').TrimStart('/');
            if (!normalized.StartsWith("Uploads/tender-lifecycle/", StringComparison.OrdinalIgnoreCase) ||
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

        public static string SafeFileName(string fileName)
        {
            return Path.GetFileName(fileName ?? String.Empty);
        }
    }
}
