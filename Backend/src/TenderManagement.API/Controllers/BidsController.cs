using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.API.Extensions;
using TenderManagement.Application.DTOs.Bid;
using TenderManagement.Application.Interfaces;
using TenderManagement.Application.Services;
using TenderManagement.Domain.Enums;

namespace TenderManagement.API.Controllers
{
    [ApiController]
    [Route("api/bids")]
    [Authorize]
    public class BidsController : ControllerBase
    {
        private readonly IBidService _bidService;
        private readonly IContractorApplicationService _applicationService;

        public BidsController(IBidService bidService, IContractorApplicationService applicationService)
        {
            _bidService = bidService;
            _applicationService = applicationService;
        }

        /// <summary>POST /api/bids/technical - contractor submits a technical bid</summary>
        [HttpPost("technical")]
        [Authorize(Roles = RoleNames.Contractor)]
        public async Task<IActionResult> SubmitTechnical([FromBody] SubmitTechnicalBidDto dto)
        {
            var result = await _bidService.SubmitTechnicalBidAsync(
                dto, User.GetUserId(), false);
            return Ok(result);
        }

        /// <summary>POST /api/bids/apply/{tenderId} - submit a contractor tender application and supporting documents.</summary>
        [HttpPost("apply/{tenderId:int}")]
        [Authorize(Roles = RoleNames.Contractor)]
        [ApiExplorerSettings(IgnoreApi = true)]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Apply(
            int tenderId,
            [FromForm] bool termsAccepted,
            [FromForm] string termsVersion,
            [FromForm] List<string>? documentTypes,
            [FromForm] List<IFormFile>? files)
        {
            if (!termsAccepted)
            {
                return BadRequest(new { message = "You must accept the Terms & Conditions before applying." });
            }

            var uploadedFiles = files ?? new List<IFormFile>();
            var uploadedTypes = documentTypes ?? new List<string>();
            if (uploadedFiles.Count > 10 || uploadedFiles.Any(file => file.Length > 10_000_000) ||
                uploadedFiles.Count != uploadedTypes.Count)
            {
                return BadRequest(new { message = "Provide a document type for each file; upload no more than 10 files of up to 10 MB each." });
            }

            var streams = uploadedFiles
                .Select((file, index) => new ContractorApplicationFile(
                    uploadedTypes[index], file.FileName, file.OpenReadStream(), file.Length))
                .ToList();
            try
            {
                var draft = await _applicationService.SaveDraftAsync(
                    tenderId, User.GetUserId(), streams, HttpContext.RequestAborted);
                var result = await _applicationService.SubmitAsync(
                    draft.TechnicalBidId, User.GetUserId(), termsAccepted, termsVersion, HttpContext.RequestAborted);
                return Ok(result);
            }
            finally
            {
                foreach (var stream in streams)
                {
                    stream.Content.Dispose();
                }
            }
        }

        /// <summary>Returns document requirements for a published tender.</summary>
        [AllowAnonymous]
        [HttpGet("applications/tender/{tenderId:int}/requirements")]
        public async Task<IActionResult> GetApplicationRequirements(int tenderId, CancellationToken cancellationToken) =>
            Ok(await _applicationService.GetRequirementsAsync(
                tenderId, !User.IsInRole(RoleNames.JuniorEngineer), cancellationToken));

        /// <summary>Replaces document requirements on a draft tender.</summary>
        [HttpPut("applications/tender/{tenderId:int}/requirements")]
        [Authorize(Roles = RoleNames.JuniorEngineer)]
        public async Task<IActionResult> SetApplicationRequirements(
            int tenderId,
            [FromBody] List<TenderApplicationRequirementDto> requirements,
            CancellationToken cancellationToken)
        {
            await _applicationService.SetRequirementsAsync(tenderId, User.GetUserId(), requirements, cancellationToken);
            return NoContent();
        }

        /// <summary>Returns the authenticated contractor's dashboard summary.</summary>
        [HttpGet("applications/dashboard")]
        [Authorize(Roles = RoleNames.Contractor)]
        public async Task<IActionResult> GetContractorDashboard(CancellationToken cancellationToken) =>
            Ok(await _applicationService.GetDashboardAsync(User.GetUserId(), cancellationToken));

        /// <summary>Marks one of the authenticated contractor's notifications as read.</summary>
        [HttpPut("applications/notifications/{notificationId:long}/read")]
        [Authorize(Roles = RoleNames.Contractor)]
        public async Task<IActionResult> MarkContractorNotificationRead(
            long notificationId, CancellationToken cancellationToken)
        {
            await _applicationService.MarkNotificationReadAsync(
                notificationId, User.GetUserId(), cancellationToken);
            return NoContent();
        }

        /// <summary>Returns the authenticated contractor's own applications.</summary>
        [HttpGet("applications/mine")]
        [Authorize(Roles = RoleNames.Contractor)]
        public async Task<IActionResult> GetMyApplications(CancellationToken cancellationToken) =>
            Ok(await _applicationService.GetMyApplicationsAsync(User.GetUserId(), cancellationToken));

        /// <summary>Returns applications visible to JE or currently assigned Tender Cell reviewers.</summary>
        [HttpGet("applications")]
        [Authorize(Roles = $"{RoleNames.JuniorEngineer},{RoleNames.TenderCell}")]
        public async Task<IActionResult> GetApplications(CancellationToken cancellationToken) =>
            Ok(await _applicationService.GetApplicationsAsync(
                User.GetUserId(), User.IsInRole(RoleNames.TenderCell), cancellationToken));

        /// <summary>Creates or updates the authenticated contractor's draft application.</summary>
        [HttpPost("applications/drafts/{tenderId:int}")]
        [Authorize(Roles = RoleNames.Contractor)]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> SaveApplicationDraft(
            int tenderId,
            [FromForm] List<string>? documentTypes,
            [FromForm] List<IFormFile>? files,
            CancellationToken cancellationToken)
        {
            var validation = CreateUploads(documentTypes, files, out var items, out var streams);
            if (validation is not null)
                return validation;
            try
            {
                var result = await _applicationService.SaveDraftAsync(
                    tenderId, User.GetUserId(), items, cancellationToken);
                return Ok(result);
            }
            finally
            {
                foreach (var stream in streams)
                    await stream.DisposeAsync();
            }
        }

        /// <summary>Finalizes a contractor's saved draft after required documents and terms are verified.</summary>
        [HttpPost("applications/{applicationId:int}/submit")]
        [Authorize(Roles = RoleNames.Contractor)]
        public async Task<IActionResult> SubmitApplication(
            int applicationId,
            [FromBody] SubmitContractorApplicationRequest request,
            CancellationToken cancellationToken) =>
            Ok(await _applicationService.SubmitAsync(
                applicationId, User.GetUserId(), request.TermsAccepted, request.TermsVersion, cancellationToken));

        /// <summary>Returns an application to its owner or an authorized internal reviewer.</summary>
        [HttpGet("applications/{applicationId:int}")]
        [Authorize(Roles = $"{RoleNames.Contractor},{RoleNames.JuniorEngineer},{RoleNames.TenderCell}")]
        public async Task<IActionResult> GetApplication(
            int applicationId, CancellationToken cancellationToken) =>
            Ok(await _applicationService.GetDetailsAsync(
                applicationId,
                User.GetUserId(),
                User.IsInRole(RoleNames.Contractor),
                User.IsInRole(RoleNames.TenderCell),
                cancellationToken));

        /// <summary>Uploads required or corrected application documents.</summary>
        [HttpPost("applications/{applicationId:int}/documents")]
        [Authorize(Roles = RoleNames.Contractor)]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> AddApplicationDocuments(
            int applicationId,
            [FromForm] List<string>? documentTypes,
            [FromForm] List<IFormFile>? files,
            CancellationToken cancellationToken)
        {
            var validation = CreateUploads(documentTypes, files, out var items, out var streams);
            if (validation is not null)
                return validation;
            try
            {
                await _applicationService.AddDocumentsAsync(
                    applicationId, User.GetUserId(), true, false, items, cancellationToken);
                return Ok(await _applicationService.GetDetailsAsync(
                    applicationId, User.GetUserId(), true, false, cancellationToken));
            }
            finally
            {
                foreach (var stream in streams)
                    await stream.DisposeAsync();
            }
        }

        /// <summary>Adds an immutable internal remark, clarification, or contractor response.</summary>
        [HttpPost("applications/{applicationId:int}/remarks")]
        [Authorize(Roles = $"{RoleNames.Contractor},{RoleNames.JuniorEngineer},{RoleNames.TenderCell}")]
        public async Task<IActionResult> AddApplicationRemark(
            int applicationId,
            [FromBody] AddContractorApplicationRemarkRequest request,
            CancellationToken cancellationToken) =>
            Ok(await _applicationService.AddRemarkAsync(
                applicationId,
                User.GetUserId(),
                User.IsInRole(RoleNames.Contractor),
                User.IsInRole(RoleNames.TenderCell),
                request,
                cancellationToken));

        /// <summary>Downloads an application document after ownership or reviewer-scope checks.</summary>
        [HttpGet("applications/{applicationId:int}/documents/{documentId:int}")]
        [Authorize(Roles = $"{RoleNames.Contractor},{RoleNames.JuniorEngineer},{RoleNames.TenderCell}")]
        public async Task<IActionResult> DownloadApplicationDocument(
            int applicationId, int documentId, CancellationToken cancellationToken)
        {
            var document = await _applicationService.OpenDocumentAsync(
                applicationId,
                documentId,
                User.GetUserId(),
                User.IsInRole(RoleNames.Contractor),
                User.IsInRole(RoleNames.TenderCell),
                cancellationToken);
            return File(document.Content, "application/octet-stream", document.FileName);
        }

        /// <summary>Records a Tender Cell document verification or shortfall decision.</summary>
        [HttpPut("applications/{applicationId:int}/documents/{documentId:int}/verification")]
        [Authorize(Roles = RoleNames.TenderCell)]
        public async Task<IActionResult> ReviewApplicationDocument(
            int applicationId,
            int documentId,
            [FromBody] ReviewApplicationDocumentRequest request,
            CancellationToken cancellationToken) =>
            Ok(await _applicationService.ReviewDocumentAsync(
                applicationId, documentId, User.GetUserId(), request, cancellationToken));

        /// <summary>Records the technical qualification decision for an application.</summary>
        [HttpPut("applications/{applicationId:int}/evaluation")]
        [Authorize(Roles = $"{RoleNames.JuniorEngineer},{RoleNames.TenderCell}")]
        public async Task<IActionResult> ReviewApplication(
            int applicationId,
            [FromBody] ReviewContractorApplicationRequest request,
            CancellationToken cancellationToken) =>
            Ok(await _applicationService.ReviewApplicationAsync(
                applicationId, User.GetUserId(), User.IsInRole(RoleNames.TenderCell), request, cancellationToken));

        private IActionResult? CreateUploads(
            List<string>? documentTypes,
            List<IFormFile>? files,
            out List<ContractorApplicationFile> uploads,
            out List<Stream> streams)
        {
            var uploadedFiles = files ?? new List<IFormFile>();
            var uploadedTypes = documentTypes ?? new List<string>();
            uploads = new List<ContractorApplicationFile>();
            streams = new List<Stream>();
            if (uploadedFiles.Count > 10 || uploadedTypes.Count != uploadedFiles.Count ||
                uploadedFiles.Any(file => file.Length <= 0 || file.Length > 10_000_000))
                return BadRequest(new { message = "Provide a document type for each non-empty file; upload no more than 10 files of up to 10 MB each." });

            var openedStreams = uploadedFiles.Select(file => file.OpenReadStream()).ToList();
            streams = openedStreams;
            uploads = uploadedFiles.Select((file, index) => new ContractorApplicationFile(
                uploadedTypes[index], file.FileName, openedStreams[index], file.Length)).ToList();
            return null;
        }

        /// <summary>PUT /api/bids/technical/evaluate - Tender Section marks Qualified/Disqualified</summary>
        [HttpPut("technical/evaluate")]
        [Authorize(Roles = RoleNames.Disabled)]
        public async Task<IActionResult> EvaluateTechnical([FromBody] EvaluateTechnicalBidDto dto)
        {
            var result = await _bidService.EvaluateTechnicalBidAsync(dto, User.GetUserId());
            return Ok(result);
        }

        /// <summary>POST /api/bids/financial - contractor submits a financial bid</summary>
        [HttpPost("financial")]
        [Authorize(Roles = RoleNames.Contractor)]
        public async Task<IActionResult> SubmitFinancial([FromBody] SubmitFinancialBidDto dto)
        {
            var result = await _bidService.SubmitFinancialBidAsync(dto, User.GetUserId());
            return Ok(result);
        }

        /// <summary>POST /api/bids/financial/je-entry - JE enters approved financial bid data.</summary>
        [HttpPost("financial/je-entry")]
        [Authorize(Roles = RoleNames.JuniorEngineer)]
        public async Task<IActionResult> SaveJeFinancialData([FromBody] SaveJeFinancialDataDto dto)
        {
            var result = await _bidService.SaveJeFinancialDataAsync(dto, User.GetUserId());
            return Ok(result);
        }

        /// <summary>POST /api/bids/financial/select - Tender Committee recommends the winning contractor</summary>
        [HttpPost("financial/select")]
        [Authorize(Roles = RoleNames.Disabled)]
        public async Task<IActionResult> SelectContractor([FromBody] SelectContractorDto dto)
        {
            var result = await _bidService.SelectContractorAsync(dto, User.GetUserId());
            return Ok(result);
        }

        /// <summary>GET /api/bids/{tenderId} - returns technical bids; financial ranking via /api/bids/{tenderId}/financial</summary>
        [HttpGet("{tenderId:int}")]
        [Authorize(Roles = $"{RoleNames.JuniorEngineer},{RoleNames.TenderCell}")]
        public async Task<IActionResult> GetTechnicalBids(int tenderId)
        {
            var result = await _bidService.GetTechnicalBidsAsync(
                tenderId, User.GetUserId(), User.IsInRole(RoleNames.TenderCell));
            return Ok(result);
        }

        /// <summary>GET /api/bids/{tenderId}/financial - ranked financial bids (L1 first)</summary>
        [HttpGet("{tenderId:int}/financial")]
        [Authorize(Roles = $"{RoleNames.JuniorEngineer},{RoleNames.TenderCommitteeChairman},{RoleNames.DirectorAuditor}")]
        public async Task<IActionResult> GetFinancialBids(int tenderId)
        {
            var result = await _bidService.GetFinancialBidsRankedAsync(tenderId);
            return Ok(result);
        }
    }
}
