namespace TenderManagement.Domain.Entities
{
    public class DesignationMaster
    {
        public string DesignationName { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
