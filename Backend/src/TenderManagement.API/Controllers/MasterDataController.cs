using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TenderManagement.API.Extensions;
using TenderManagement.Application.Common;
using TenderManagement.Application.DTOs.Role;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;
using TenderManagement.Infrastructure.Data;

namespace TenderManagement.API.Controllers
{
    [ApiController]
    [Route("api/master-data")]
    [Authorize(Policy = "masterdata.manage")]
    public sealed class MasterDataController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public MasterDataController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        /// <summary>Returns a searchable, paged catalog from SQL Server.</summary>
        [HttpGet("{catalog}/paged")]
        public async Task<ActionResult<PagedResult<MasterDataItemResponse>>> GetPaged(
            string catalog,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            CancellationToken cancellationToken = default)
        {
            var query = GetCatalogQuery(catalog);
            if (query is null)
                return NotFound(new { message = "Unknown master-data catalog." });

            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(item => item.Name.Contains(term));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            return Ok(new PagedResult<MasterDataItemResponse>
            {
                Items = await query
                    .OrderBy(item => item.SortOrder)
                    .ThenBy(item => item.Name)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        /// <summary>Creates a master-data item in an existing SQL catalog.</summary>
        [HttpPost("{catalog}")]
        public async Task<ActionResult<MasterDataItemResponse>> Create(
            string catalog,
            [FromBody] SaveMasterDataItemRequest request,
            CancellationToken cancellationToken)
        {
            var normalizedCatalog = NormalizeCatalog(catalog);
            if (normalizedCatalog is null)
                return NotFound(new { message = "Unknown master-data catalog." });

            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Name is required." });
            if (name.Length > GetCatalogNameLimit(normalizedCatalog))
                return BadRequest(new { message = $"Name must be {GetCatalogNameLimit(normalizedCatalog)} characters or fewer." });
            if (await CatalogContainsNameAsync(normalizedCatalog, name, cancellationToken))
                return Conflict(new { message = "An item with this name already exists." });

            var item = AddCatalogItem(normalizedCatalog, name, request.SortOrder, request.IsActive);
            if (item is null)
                return NotFound(new { message = "Unknown master-data catalog." });

            await _context.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync(User.GetUserId(), "Create", $"MasterData:{normalizedCatalog}", null, name, cancellationToken);
            return CreatedAtAction(
                nameof(GetPaged),
                new { catalog = normalizedCatalog, pageNumber = 1, pageSize = 10 },
                item);
        }

        /// <summary>Updates the display key only when no historical record references it.</summary>
        [HttpPut("{catalog}/{key}")]
        public async Task<ActionResult<MasterDataItemResponse>> Update(
            string catalog,
            string key,
            [FromBody] SaveMasterDataItemRequest request,
            CancellationToken cancellationToken)
        {
            var normalizedCatalog = NormalizeCatalog(catalog);
            if (normalizedCatalog is null)
                return NotFound(new { message = "Unknown master-data catalog." });

            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Name is required." });
            if (name.Length > GetCatalogNameLimit(normalizedCatalog))
                return BadRequest(new { message = $"Name must be {GetCatalogNameLimit(normalizedCatalog)} characters or fewer." });
            var current = await FindCatalogItemAsync(normalizedCatalog, key, cancellationToken);
            if (current is null)
                return NotFound();

            if (!string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
            {
                if (await CatalogContainsNameAsync(normalizedCatalog, name, cancellationToken))
                    return Conflict(new { message = "An item with this name already exists." });
                if (await IsCatalogItemInUseAsync(normalizedCatalog, key, cancellationToken))
                    return Conflict(new { message = "This item is referenced by existing records. Deactivate it instead of renaming it." });
            }

            var updated = UpdateCatalogItem(normalizedCatalog, key, name, request.SortOrder, request.IsActive);
            if (updated is null)
                return NotFound();

            await _context.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync(User.GetUserId(), "Update", $"MasterData:{normalizedCatalog}", null, $"{key} -> {name}", cancellationToken);
            return Ok(updated);
        }

        /// <summary>Deletes an unused item or deactivates it when history references it.</summary>
        [HttpDelete("{catalog}/{key}")]
        public async Task<IActionResult> Delete(string catalog, string key, CancellationToken cancellationToken)
        {
            var normalizedCatalog = NormalizeCatalog(catalog);
            if (normalizedCatalog is null)
                return NotFound(new { message = "Unknown master-data catalog." });

            var inUse = await IsCatalogItemInUseAsync(normalizedCatalog, key, cancellationToken);
            var deleted = await DeleteCatalogItemAsync(normalizedCatalog, key, deactivate: inUse, cancellationToken);
            if (!deleted)
                return NotFound();

            await _context.SaveChangesAsync(cancellationToken);
            var action = inUse ? "Deactivate" : "Delete";
            await _auditService.LogAsync(User.GetUserId(), action, $"MasterData:{normalizedCatalog}", null, key, cancellationToken);
            return Ok(new
            {
                message = inUse
                    ? "This item is referenced by existing records, so it was deactivated."
                    : "Master-data item deleted."
            });
        }

        private IQueryable<MasterDataItemResponse>? GetCatalogQuery(string catalog) =>
            NormalizeCatalog(catalog) switch
            {
                "tender-methods" => _context.TenderMethods.AsNoTracking()
                    .Select(item => new MasterDataItemResponse(item.MethodName, item.SortOrder, item.IsActive)),
                "tender-types" => _context.TenderCategories.AsNoTracking()
                    .Select(item => new MasterDataItemResponse(item.TypeName, item.SortOrder, item.IsActive)),
                "document-types" => _context.DocumentTypes.AsNoTracking()
                    .Select(item => new MasterDataItemResponse(item.TypeName, item.SortOrder, item.IsActive)),
                "units" => _context.Units.AsNoTracking()
                    .Select(item => new MasterDataItemResponse(item.UnitName, item.SortOrder, item.IsActive)),
                _ => null
            };

        private async Task<bool> CatalogContainsNameAsync(
            string catalog,
            string name,
            CancellationToken cancellationToken) =>
            catalog switch
            {
                "tender-methods" => await _context.TenderMethods.AnyAsync(item => item.MethodName == name, cancellationToken),
                "tender-types" => await _context.TenderCategories.AnyAsync(item => item.TypeName == name, cancellationToken),
                "document-types" => await _context.DocumentTypes.AnyAsync(item => item.TypeName == name, cancellationToken),
                "units" => await _context.Units.AnyAsync(item => item.UnitName == name, cancellationToken),
                _ => false
            };

        private object? AddCatalogItem(string catalog, string name, int sortOrder, bool isActive)
        {
            switch (catalog)
            {
                case "tender-methods":
                    var method = new TenderMethod { MethodName = name, SortOrder = sortOrder, IsActive = isActive };
                    _context.TenderMethods.Add(method);
                    return new MasterDataItemResponse(name, sortOrder, isActive);
                case "tender-types":
                    var tenderType = new TenderCategoryMaster { TypeName = name, SortOrder = sortOrder, IsActive = isActive };
                    _context.TenderCategories.Add(tenderType);
                    return new MasterDataItemResponse(name, sortOrder, isActive);
                case "document-types":
                    var documentType = new DocumentTypeMaster { TypeName = name, SortOrder = sortOrder, IsActive = isActive };
                    _context.DocumentTypes.Add(documentType);
                    return new MasterDataItemResponse(name, sortOrder, isActive);
                case "units":
                    var unit = new UnitMaster { UnitName = name, SortOrder = sortOrder, IsActive = isActive };
                    _context.Units.Add(unit);
                    return new MasterDataItemResponse(name, sortOrder, isActive);
                default:
                    return null;
            }
        }

        private async Task<MasterDataItemResponse?> FindCatalogItemAsync(
            string catalog,
            string key,
            CancellationToken cancellationToken)
        {
            var query = GetCatalogQuery(catalog);
            return query is null
                ? null
                : await query.SingleOrDefaultAsync(item => item.Name == key, cancellationToken);
        }

        private object? UpdateCatalogItem(string catalog, string key, string name, int sortOrder, bool isActive)
        {
            switch (catalog)
            {
                case "tender-methods":
                    var method = _context.TenderMethods.Find(key);
                    if (method is null) return null;
                    method.MethodName = name;
                    method.SortOrder = sortOrder;
                    method.IsActive = isActive;
                    return new MasterDataItemResponse(name, sortOrder, isActive);
                case "tender-types":
                    var tenderType = _context.TenderCategories.Find(key);
                    if (tenderType is null) return null;
                    tenderType.TypeName = name;
                    tenderType.SortOrder = sortOrder;
                    tenderType.IsActive = isActive;
                    return new MasterDataItemResponse(name, sortOrder, isActive);
                case "document-types":
                    var documentType = _context.DocumentTypes.Find(key);
                    if (documentType is null) return null;
                    documentType.TypeName = name;
                    documentType.SortOrder = sortOrder;
                    documentType.IsActive = isActive;
                    return new MasterDataItemResponse(name, sortOrder, isActive);
                case "units":
                    var unit = _context.Units.Find(key);
                    if (unit is null) return null;
                    unit.UnitName = name;
                    unit.SortOrder = sortOrder;
                    unit.IsActive = isActive;
                    return new MasterDataItemResponse(name, sortOrder, isActive);
                default:
                    return null;
            }
        }

        private async Task<bool> IsCatalogItemInUseAsync(
            string catalog,
            string key,
            CancellationToken cancellationToken) =>
            catalog switch
            {
                "tender-methods" => await _context.Tenders.IgnoreQueryFilters()
                    .AnyAsync(tender => tender.TenderType == key, cancellationToken),
                "tender-types" => await _context.Tenders.IgnoreQueryFilters()
                    .AnyAsync(tender => tender.TenderCategoryName == key, cancellationToken),
                "document-types" =>
                    await _context.TechnicalBidDocuments.AnyAsync(document => document.DocumentType == key, cancellationToken) ||
                    await _context.Database.SqlQuery<long>($"""
                        SELECT COUNT_BIG(*) AS [Value]
                        FROM dbo.TenderRequiredBidDocuments
                        WHERE DocumentType={key}
                        """).SingleAsync(cancellationToken) > 0 ||
                    await _context.Database.SqlQuery<long>($"""
                        SELECT COUNT_BIG(*) AS [Value]
                        FROM dbo.TenderShortfallItems
                        WHERE RequiredDocumentType={key}
                        """).SingleAsync(cancellationToken) > 0,
                "units" => await _context.BOQItems.AnyAsync(item => item.Unit == key, cancellationToken),
                _ => false
            };

        private async Task<bool> DeleteCatalogItemAsync(
            string catalog,
            string key,
            bool deactivate,
            CancellationToken cancellationToken)
        {
            switch (catalog)
            {
                case "tender-methods":
                    var method = await _context.TenderMethods.FindAsync(new object[] { key }, cancellationToken);
                    if (method is null) return false;
                    if (deactivate) method.IsActive = false;
                    else _context.TenderMethods.Remove(method);
                    return true;
                case "tender-types":
                    var tenderType = await _context.TenderCategories.FindAsync(new object[] { key }, cancellationToken);
                    if (tenderType is null) return false;
                    if (deactivate) tenderType.IsActive = false;
                    else _context.TenderCategories.Remove(tenderType);
                    return true;
                case "document-types":
                    var documentType = await _context.DocumentTypes.FindAsync(new object[] { key }, cancellationToken);
                    if (documentType is null) return false;
                    if (deactivate) documentType.IsActive = false;
                    else _context.DocumentTypes.Remove(documentType);
                    return true;
                case "units":
                    var unit = await _context.Units.FindAsync(new object[] { key }, cancellationToken);
                    if (unit is null) return false;
                    if (deactivate) unit.IsActive = false;
                    else _context.Units.Remove(unit);
                    return true;
                default:
                    return false;
            }
        }

        private static string? NormalizeCatalog(string catalog) =>
            catalog.Trim().ToLowerInvariant() switch
            {
                "tender-methods" => "tender-methods",
                "tender-types" => "tender-types",
                "document-types" => "document-types",
                "units" => "units",
                _ => null
            };

        private static int GetCatalogNameLimit(string catalog) =>
            catalog switch
            {
                "tender-methods" or "tender-types" => 50,
                "document-types" => 80,
                "units" => 20,
                _ => throw new ArgumentOutOfRangeException(nameof(catalog), catalog, "Unknown master-data catalog.")
            };
    }
}
