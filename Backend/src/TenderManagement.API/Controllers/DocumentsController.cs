using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.API.Extensions;
using TenderManagement.API.Models;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Enums;

namespace TenderManagement.API.Controllers
{
    [ApiController]
    [Route("api/documents")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly ITenderService _tenderService;

        public DocumentsController(ITenderService tenderService)
        {
            _tenderService = tenderService;
        }

        /// <summary>POST /api/documents/upload - multipart form: tenderId, documentType, file</summary>
        [HttpPost("upload")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Upload([FromForm] UploadDocumentRequest request)
        {
            if (request.File is null || request.File.Length == 0) return BadRequest("No file supplied.");

            using var stream = request.File.OpenReadStream();
            var result = await _tenderService.UploadDocumentAsync(
                request.TenderId, request.DocumentType, stream, request.File.FileName, User.GetUserId());
            return Ok(result);
        }

        /// <summary>GET /api/documents/{tenderId}</summary>
        [HttpGet("{tenderId:int}")]
        public async Task<IActionResult> GetByTender(int tenderId)
        {
            var result = await _tenderService.GetDocumentsAsync(tenderId);
            return Ok(result);
        }

        /// <summary>DELETE /api/documents/{documentId}</summary>
        [HttpDelete("{documentId:int}")]
        [Authorize(Roles = $"{RoleNames.DepartmentOfficer},{RoleNames.Admin}")]
        public async Task<IActionResult> Delete(int documentId)
        {
            await _tenderService.DeleteDocumentAsync(documentId, User.GetUserId());
            return NoContent();
        }
    }
}
