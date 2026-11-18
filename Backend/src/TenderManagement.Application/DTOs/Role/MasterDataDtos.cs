using System.ComponentModel.DataAnnotations;

namespace TenderManagement.Application.DTOs.Role
{
    /// <summary>A database-backed item in an administrative master-data catalog.</summary>
    public sealed record MasterDataItemResponse(
        string Name,
        int SortOrder,
        bool IsActive);

    /// <summary>Payload for creating or updating one master-data catalog item.</summary>
    public sealed record SaveMasterDataItemRequest
    {
        [Required, StringLength(150, MinimumLength = 1)]
        public required string Name { get; init; }

        [Range(0, 100000)]
        public int SortOrder { get; init; }

        public bool IsActive { get; init; } = true;
    }
}
