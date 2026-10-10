namespace TenderManagement.Domain.Entities
{
    public class Department
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? DepartmentCode { get; set; }
        public string? HeadOfficerName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Tender> Tenders { get; set; } = new List<Tender>();
    }
}
