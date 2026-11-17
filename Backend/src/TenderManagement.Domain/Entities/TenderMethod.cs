namespace TenderManagement.Domain.Entities
{
    public class TenderMethod
    {
        public string MethodName { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
