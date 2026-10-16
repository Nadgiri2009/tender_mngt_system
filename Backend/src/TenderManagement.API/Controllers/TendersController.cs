using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.API.Extensions;
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
            int? ownerId = null;
            if (User.IsInRole(RoleNames.DepartmentOfficer))
            {
                ownerId = User.GetUserId();
            }
            else if (User.Identity?.IsAuthenticated != true || User.IsInRole(RoleNames.Contractor))
            {
                query.Status = TenderStatus.TenderPublished;
            }
            var result = await _tenderService.GetTendersAsync(query, ownerId);
            return Ok(result);
        }

        /// <summary>GET /api/tenders/{id} - public users can view published tender notices only</summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            var isContractor = User.IsInRole(RoleNames.Contractor);
            var ownerId = isAuthenticated && User.IsInRole(RoleNames.DepartmentOfficer)
                ? User.GetUserId()
                : (int?)null;
            var result = await _tenderService.GetTenderByIdAsync(
                id,
                ownerId,
                allowPublishedForOtherOwners: isAuthenticated && ownerId.HasValue);
            if ((!isAuthenticated || isContractor) && result.CurrentStatus != TenderStatus.TenderPublished)
            {
                return NotFound();
            }

            if (!isAuthenticated || isContractor)
            {
                result.AssignedRespOfficer = string.Empty;
                result.AssignedRole = null;
                result.CreatedByName = string.Empty;
            }

            return Ok(result);
        }

        /// <summary>POST /api/tenders - Department officers and admins create tender requirements (multipart: json + files)</summary>
        [HttpPost]
        [Authorize(Roles = $"{RoleNames.DepartmentOfficer},{RoleNames.Admin}")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Create([FromForm] string tenderJson, [FromForm] List<IFormFile>? files)
        {
            if (string.IsNullOrWhiteSpace(tenderJson)) return BadRequest("tenderJson is required");

            var dto = System.Text.Json.JsonSerializer.Deserialize<CreateTenderDto>(tenderJson, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dto is null) return BadRequest("Invalid tender payload.");
            if (!TryValidateModel(dto)) return BadRequest(ModelState);

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

            return CreatedAtAction(nameof(GetById), new { id = result.TenderId }, result);
        }

        /// <summary>PUT /api/tenders/{id} - Admin edits tender while still in Draft</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTenderDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _tenderService.UpdateTenderAsync(id, dto, User.GetUserId());
            return Ok(result);
        }

        /// <summary>DELETE /api/tenders/{id} - soft delete, Admin only, editable only while still in Draft</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            await _tenderService.DeleteTenderAsync(id, User.GetUserId());
            return NoContent();
        }

    }
}
