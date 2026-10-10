namespace TenderManagement.Domain.Common
{
    /// <summary>
    /// Base class providing common audit columns for entities that need them.
    /// </summary>
    public abstract class AuditableEntity
    {
        public int? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
