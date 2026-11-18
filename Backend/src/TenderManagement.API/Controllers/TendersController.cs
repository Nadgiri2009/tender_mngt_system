using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.API.Extensions;
using TenderManagement.Application.Common;
using TenderManagement.Application.DTOs.Common;
using TenderManagement.Application.DTOs.Tender;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Enums;

namespace TenderManagement.API.Controllers
{
    [ApiController]
    [Route("api/tenders")]
    [Authorize]
    public class TendersController : ControllerBase
    {
        private readonly ITenderService _tenderService;

        public TendersController(ITenderService tenderService)
        {
            _tenderService = tenderService;
        }

        /// <summary>GET /api/tenders - paginated, searchable, filterable list</summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] PaginationQuery query)
        {
            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            var isExternalBidder = User.IsInRole(RoleNames.ExternalBidder) ||
                User.FindFirst("accountType")?.Value == "ExternalBidder";
            IReadOnlyCollection<int>? allowedTenderIds = null;

            if (!isAuthenticated || isExternalBidder)
            {
                query.Status = TenderStatus.TenderPublished;
            }
            else if (!User.IsInRole(RoleNames.JuniorEngineer))
            {
                var roleIdValue = User.FindFirst("roleId")?.Value;
                if (!int.TryParse(roleIdValue, out var roleId))
                    return Forbid();

                allowedTenderIds = await _tenderService.GetAssignedTenderIdsAsync(User.GetUserId(), roleId);
            }

            var result = await _tenderService.GetTendersAsync(query, allowedTenderIds: allowedTenderIds);
            return Ok(result);
        }

        /// <summary>GET /api/tenders/{id} - public users can view published tender notices only</summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            var isExternalBidder = User.IsInRole(RoleNames.ExternalBidder) ||
                User.FindFirst("accountType")?.Value == "ExternalBidder";
            if (isAuthenticated && !isExternalBidder && !User.IsInRole(RoleNames.JuniorEngineer))
            {
                var roleIdValue = User.FindFirst("roleId")?.Value;
                if (!int.TryParse(roleIdValue, out var roleId))
                    return Forbid();

                var allowedTenderIds = await _tenderService.GetAssignedTenderIdsAsync(User.GetUserId(), roleId);
                if (!allowedTenderIds.Contains(id))
                    return NotFound();
            }

            var result = await _tenderService.GetTenderByIdAsync(
                id,
                createdByUserId: null,
                allowPublishedForOtherOwners: false);
            if ((!isAuthenticated || isExternalBidder) && result.CurrentStatus != TenderStatus.TenderPublished)
            {
                return NotFound();
            }

            if (!isAuthenticated || isExternalBidder)
            {
                result.AssignedRespOfficer = string.Empty;
                result.AssignedRole = null;
                result.CreatedByName = string.Empty;
            }

            return Ok(result);
        }

        /// <summary>POST /api/tenders - Junior Engineer (JE) Main Admin creates a tender (multipart: json + files)</summary>
        [HttpPost]
        [Authorize(Roles = RoleNames.JuniorEngineer)]
        [RequestSizeLimit(DocumentUploadRules.MaxRequestBytes)]
        public async Task<IActionResult> Create(
            [FromForm] string tenderJson,
            [FromForm] List<IFormFile>? files,
            [FromForm] IFormFile? preBidDocument)
        {
            if (string.IsNullOrWhiteSpace(tenderJson)) return BadRequest("tenderJson is required");

            CreateTenderDto? dto;
            try
            {
                dto = System.Text.Json.JsonSerializer.Deserialize<CreateTenderDto>(tenderJson, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (System.Text.Json.JsonException)
            {
                return BadRequest("Invalid tender payload.");
            }

            if (dto is null) return BadRequest("Invalid tender payload.");
            if (!TryValidateModel(dto)) return ValidationProblem(ModelState);

            if (dto.PreBidMeetingUtc.HasValue && preBidDocument is null)
            {
                ModelState.AddModelError(nameof(dto.PreBidMeetingUtc), "Upload the pre-bid meeting suggestion when a meeting is scheduled.");
            }

            var uploads = (files ?? new List<IFormFile>())
                .Concat(preBidDocument is null ? Enumerable.Empty<IFormFile>() : new[] { preBidDocument });
            foreach (var file in uploads)
            {
                var extension = Path.GetExtension(file.FileName);
                if (!DocumentUploadRules.AllowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("files", $"File type '{extension}' is not permitted.");
                }
                if (file.Length <= 0 || file.Length > DocumentUploadRules.MaxRequestBytes)
                {
                    ModelState.AddModelError("files", "Each uploaded document must be between 1 byte and 50 MB.");
                }
            }

            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var result = await _tenderService.CreateTenderAsync(dto, User.GetUserId());

            // Save any uploaded files as TenderDocuments linked to the created tender
            if (files != null && files.Any())
            {
                foreach (var file in files)
                {
                    using var stream = file.OpenReadStream();
                    // Use provided file name and a generic document type 'Attachment'
                    await _tenderService.UploadDocumentAsync(result.TenderId, "Attachment", stream, file.FileName, User.GetUserId());
                }
            }

            if (preBidDocument is not null)
            {
                using var stream = preBidDocument.OpenReadStream();
                await _tenderService.UploadDocumentAsync(
                    result.TenderId,
                    "Pre-Bid Meeting Suggestion",
                    stream,
                    preBidDocument.FileName,
                    User.GetUserId());
            }

            return CreatedAtAction(nameof(GetById), new { id = result.TenderId }, result);
        }

        /// <summary>PUT /api/tenders/{id} - Admin edits tender while still in Draft</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleNames.JuniorEngineer)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTenderDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _tenderService.UpdateTenderAsync(id, dto, User.GetUserId());
            return Ok(result);
        }

        /// <summary>DELETE /api/tenders/{id} - soft delete, Admin only, editable only while still in Draft</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleNames.JuniorEngineer)]
        public async Task<IActionResult> Delete(int id)
        {
            await _tenderService.DeleteTenderAsync(id, User.GetUserId());
            return NoContent();
        }

    }
}
