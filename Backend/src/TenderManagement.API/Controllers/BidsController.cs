using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.API.Extensions;
using TenderManagement.Application.DTOs.Bid;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Enums;

namespace TenderManagement.API.Controllers
{
    [ApiController]
    [Route("api/bids")]
    [Authorize]
    public class BidsController : ControllerBase
    {
        private readonly IBidService _bidService;

        public BidsController(IBidService bidService)
        {
            _bidService = bidService;
        }

        /// <summary>POST /api/bids/technical - contractor submits a technical bid</summary>
        [HttpPost("technical")]
        [Authorize(Roles = $"{RoleNames.Contractor},{RoleNames.Admin}")]
        public async Task<IActionResult> SubmitTechnical([FromBody] SubmitTechnicalBidDto dto)
        {
            var result = await _bidService.SubmitTechnicalBidAsync(dto);
            return Ok(result);
        }

        /// <summary>PUT /api/bids/technical/evaluate - Tender Section marks Qualified/Disqualified</summary>
        [HttpPut("technical/evaluate")]
        [Authorize(Roles = $"{RoleNames.TenderSection},{RoleNames.Admin}")]
        public async Task<IActionResult> EvaluateTechnical([FromBody] EvaluateTechnicalBidDto dto)
        {
            var result = await _bidService.EvaluateTechnicalBidAsync(dto, User.GetUserId());
            return Ok(result);
        }

        /// <summary>POST /api/bids/financial - contractor submits a financial bid</summary>
        [HttpPost("financial")]
        [Authorize(Roles = $"{RoleNames.Contractor},{RoleNames.Admin}")]
        public async Task<IActionResult> SubmitFinancial([FromBody] SubmitFinancialBidDto dto)
        {
            var result = await _bidService.SubmitFinancialBidAsync(dto);
            return Ok(result);
        }

        /// <summary>POST /api/bids/financial/select - Tender Committee recommends the winning contractor</summary>
        [HttpPost("financial/select")]
        [Authorize(Roles = $"{RoleNames.TenderCommittee},{RoleNames.Admin}")]
        public async Task<IActionResult> SelectContractor([FromBody] SelectContractorDto dto)
        {
            var result = await _bidService.SelectContractorAsync(dto, User.GetUserId());
            return Ok(result);
        }

        /// <summary>GET /api/bids/{tenderId} - returns technical bids; financial ranking via /api/bids/{tenderId}/financial</summary>
        [HttpGet("{tenderId:int}")]
        public async Task<IActionResult> GetTechnicalBids(int tenderId)
        {
            var result = await _bidService.GetTechnicalBidsAsync(tenderId);
            return Ok(result);
        }

        /// <summary>GET /api/bids/{tenderId}/financial - ranked financial bids (L1 first)</summary>
        [HttpGet("{tenderId:int}/financial")]
        public async Task<IActionResult> GetFinancialBids(int tenderId)
        {
            var result = await _bidService.GetFinancialBidsRankedAsync(tenderId);
            return Ok(result);
        }
    }
}
