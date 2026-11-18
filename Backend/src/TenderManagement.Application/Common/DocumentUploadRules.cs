namespace TenderManagement.Application.Common
{
    public static class DocumentUploadRules
    {
        public const long MaxRequestBytes = 50_000_000;

        public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png"
        };
    }
}
