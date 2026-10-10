using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.Application.DTOs.Tender;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Enums;

namespace TenderManagement.API.Controllers
{
    [ApiController]
    [Route("api/boq")]
    [Authorize]
    public class BOQController : ControllerBase
    {
        private readonly ITenderService _tenderService;

        public BOQController(ITenderService tenderService)
        {
            _tenderService = tenderService;
        }

        /// <summary>POST /api/boq</summary>
        [HttpPost]
        [Authorize(Roles = $"{RoleNames.DepartmentOfficer},{RoleNames.Admin}")]
        public async Task<IActionResult> Create([FromBody] CreateBOQItemDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _tenderService.AddBOQItemAsync(dto);
            return Ok(result);
        }

        /// <summary>GET /api/boq/tender/{tenderId}</summary>
        [HttpGet("tender/{tenderId:int}")]
        public async Task<IActionResult> GetByTender(int tenderId)
        {
            var result = await _tenderService.GetBOQItemsAsync(tenderId);
            return Ok(result);
        }

        /// <summary>PUT /api/boq/{id}</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{RoleNames.DepartmentOfficer},{RoleNames.Admin}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBOQItemDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _tenderService.UpdateBOQItemAsync(id, dto);
            return Ok(result);
        }

        /// <summary>DELETE /api/boq/{id}</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{RoleNames.DepartmentOfficer},{RoleNames.Admin}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _tenderService.DeleteBOQItemAsync(id);
            return NoContent();
        }
    }
}
