namespace TenderManagement.Domain.Entities
{
    public class UnitMaster
    {
        public string UnitName { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
