namespace TenderManagement.Domain.Entities
{
    public class DocumentTypeMaster
    {
        public string TypeName { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
