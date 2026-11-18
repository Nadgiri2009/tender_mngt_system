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
            var normalizedCatalog = NormalizeCatalog(catalog);
            if (normalizedCatalog is null)
                return NotFound(new { message = "Unknown master-data catalog." });

            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            switch (normalizedCatalog)
            {
                case "tender-methods":
                {
                    var query = _context.TenderMethods.AsNoTracking();
                    if (term is not null) query = query.Where(item => item.MethodName.Contains(term));
                    var totalCount = await query.CountAsync(cancellationToken);
                    var items = await query.OrderBy(item => item.SortOrder).ThenBy(item => item.MethodName)
                        .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                        .Select(item => new MasterDataItemResponse(item.MethodName, item.SortOrder, item.IsActive))
                        .ToListAsync(cancellationToken);
                    return Ok(CreatePage(items, totalCount, pageNumber, pageSize));
                }
                case "tender-types":
                {
                    var query = _context.TenderCategories.AsNoTracking();
                    if (term is not null) query = query.Where(item => item.TypeName.Contains(term));
                    var totalCount = await query.CountAsync(cancellationToken);
                    var items = await query.OrderBy(item => item.SortOrder).ThenBy(item => item.TypeName)
                        .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                        .Select(item => new MasterDataItemResponse(item.TypeName, item.SortOrder, item.IsActive))
                        .ToListAsync(cancellationToken);
                    return Ok(CreatePage(items, totalCount, pageNumber, pageSize));
                }
                case "designations":
                {
                    var query = _context.Designations.AsNoTracking();
                    if (term is not null) query = query.Where(item => item.DesignationName.Contains(term));
                    var totalCount = await query.CountAsync(cancellationToken);
                    var items = await query.OrderBy(item => item.SortOrder).ThenBy(item => item.DesignationName)
                        .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                        .Select(item => new MasterDataItemResponse(item.DesignationName, item.SortOrder, item.IsActive))
                        .ToListAsync(cancellationToken);
                    return Ok(CreatePage(items, totalCount, pageNumber, pageSize));
                }
                case "document-types":
                {
                    var query = _context.DocumentTypes.AsNoTracking();
                    if (term is not null) query = query.Where(item => item.TypeName.Contains(term));
                    var totalCount = await query.CountAsync(cancellationToken);
                    var items = await query.OrderBy(item => item.SortOrder).ThenBy(item => item.TypeName)
                        .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                        .Select(item => new MasterDataItemResponse(item.TypeName, item.SortOrder, item.IsActive))
                        .ToListAsync(cancellationToken);
                    return Ok(CreatePage(items, totalCount, pageNumber, pageSize));
                }
                case "units":
                {
                    var query = _context.Units.AsNoTracking();
                    if (term is not null) query = query.Where(item => item.UnitName.Contains(term));
                    var totalCount = await query.CountAsync(cancellationToken);
                    var items = await query.OrderBy(item => item.SortOrder).ThenBy(item => item.UnitName)
                        .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                        .Select(item => new MasterDataItemResponse(item.UnitName, item.SortOrder, item.IsActive))
                        .ToListAsync(cancellationToken);
                    return Ok(CreatePage(items, totalCount, pageNumber, pageSize));
                }
                default:
                    return NotFound(new { message = "Unknown master-data catalog." });
            }
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

            if (!string.Equals(key, name, StringComparison.Ordinal))
            {
                if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = "Changing only the letter casing of a catalog key is not supported." });
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

        private async Task<bool> CatalogContainsNameAsync(
            string catalog,
            string name,
            CancellationToken cancellationToken) =>
            catalog switch
            {
                "tender-methods" => await _context.TenderMethods.AnyAsync(item => item.MethodName == name, cancellationToken),
                "tender-types" => await _context.TenderCategories.AnyAsync(item => item.TypeName == name, cancellationToken),
                "designations" => await _context.Designations.AnyAsync(item => item.DesignationName == name, cancellationToken),
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
                case "designations":
                    var designation = new DesignationMaster
                    {
                        DesignationName = name,
                        SortOrder = sortOrder,
                        IsActive = isActive,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    _context.Designations.Add(designation);
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
            CancellationToken cancellationToken) =>
            catalog switch
            {
                "tender-methods" => await _context.TenderMethods.AsNoTracking()
                    .Where(item => item.MethodName == key)
                    .Select(item => new MasterDataItemResponse(item.MethodName, item.SortOrder, item.IsActive))
                    .SingleOrDefaultAsync(cancellationToken),
                "tender-types" => await _context.TenderCategories.AsNoTracking()
                    .Where(item => item.TypeName == key)
                    .Select(item => new MasterDataItemResponse(item.TypeName, item.SortOrder, item.IsActive))
                    .SingleOrDefaultAsync(cancellationToken),
                "designations" => await _context.Designations.AsNoTracking()
                    .Where(item => item.DesignationName == key)
                    .Select(item => new MasterDataItemResponse(item.DesignationName, item.SortOrder, item.IsActive))
                    .SingleOrDefaultAsync(cancellationToken),
                "document-types" => await _context.DocumentTypes.AsNoTracking()
                    .Where(item => item.TypeName == key)
                    .Select(item => new MasterDataItemResponse(item.TypeName, item.SortOrder, item.IsActive))
                    .SingleOrDefaultAsync(cancellationToken),
                "units" => await _context.Units.AsNoTracking()
                    .Where(item => item.UnitName == key)
                    .Select(item => new MasterDataItemResponse(item.UnitName, item.SortOrder, item.IsActive))
                    .SingleOrDefaultAsync(cancellationToken),
                _ => null
            };

        private static PagedResult<MasterDataItemResponse> CreatePage(
            List<MasterDataItemResponse> items,
            int totalCount,
            int pageNumber,
            int pageSize) =>
            new()
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

        private object? UpdateCatalogItem(string catalog, string key, string name, int sortOrder, bool isActive)
        {
            switch (catalog)
            {
                case "tender-methods":
                    var method = _context.TenderMethods.Find(key);
                    if (method is null) return null;
                    if (!string.Equals(key, name, StringComparison.Ordinal))
                    {
                        _context.TenderMethods.Remove(method);
                        _context.TenderMethods.Add(new TenderMethod
                        {
                            MethodName = name,
                            SortOrder = sortOrder,
                            IsActive = isActive
                        });
                    }
                    else
                    {
                        method.SortOrder = sortOrder;
                        method.IsActive = isActive;
                    }
                    return new MasterDataItemResponse(name, sortOrder, isActive);
                case "tender-types":
                    var tenderType = _context.TenderCategories.Find(key);
                    if (tenderType is null) return null;
                    if (!string.Equals(key, name, StringComparison.Ordinal))
                    {
                        _context.TenderCategories.Remove(tenderType);
                        _context.TenderCategories.Add(new TenderCategoryMaster
                        {
                            TypeName = name,
                            SortOrder = sortOrder,
                            IsActive = isActive
                        });
                    }
                    else
                    {
                        tenderType.SortOrder = sortOrder;
                        tenderType.IsActive = isActive;
                    }
                    return new MasterDataItemResponse(name, sortOrder, isActive);
                case "designations":
                    var designation = _context.Designations.Find(key);
                    if (designation is null) return null;
                    if (!string.Equals(key, name, StringComparison.Ordinal))
                    {
                        _context.Designations.Remove(designation);
                        _context.Designations.Add(new DesignationMaster
                        {
                            DesignationName = name,
                            SortOrder = sortOrder,
                            IsActive = isActive,
                            CreatedAtUtc = designation.CreatedAtUtc
                        });
                    }
                    else
                    {
                        designation.SortOrder = sortOrder;
                        designation.IsActive = isActive;
                    }
                    return new MasterDataItemResponse(name, sortOrder, isActive);
                case "document-types":
                    var documentType = _context.DocumentTypes.Find(key);
                    if (documentType is null) return null;
                    if (!string.Equals(key, name, StringComparison.Ordinal))
                    {
                        _context.DocumentTypes.Remove(documentType);
                        _context.DocumentTypes.Add(new DocumentTypeMaster
                        {
                            TypeName = name,
                            SortOrder = sortOrder,
                            IsActive = isActive
                        });
                    }
                    else
                    {
                        documentType.SortOrder = sortOrder;
                        documentType.IsActive = isActive;
                    }
                    return new MasterDataItemResponse(name, sortOrder, isActive);
                case "units":
                    var unit = _context.Units.Find(key);
                    if (unit is null) return null;
                    if (!string.Equals(key, name, StringComparison.Ordinal))
                    {
                        _context.Units.Remove(unit);
                        _context.Units.Add(new UnitMaster
                        {
                            UnitName = name,
                            SortOrder = sortOrder,
                            IsActive = isActive
                        });
                    }
                    else
                    {
                        unit.SortOrder = sortOrder;
                        unit.IsActive = isActive;
                    }
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
                "designations" => false,
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
                case "designations":
                    var designation = await _context.Designations.FindAsync(new object[] { key }, cancellationToken);
                    if (designation is null) return false;
                    if (deactivate) designation.IsActive = false;
                    else _context.Designations.Remove(designation);
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
                "designations" => "designations",
                "document-types" => "document-types",
                "units" => "units",
                _ => null
            };

        private static int GetCatalogNameLimit(string catalog) =>
            catalog switch
            {
                "tender-methods" or "tender-types" => 50,
                "designations" => 150,
                "document-types" => 80,
                "units" => 20,
                _ => throw new ArgumentOutOfRangeException(nameof(catalog), catalog, "Unknown master-data catalog.")
            };
    }
}
