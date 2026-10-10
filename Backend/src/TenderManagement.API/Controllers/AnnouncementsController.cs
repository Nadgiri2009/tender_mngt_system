using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.API.Extensions;
using TenderManagement.Application.DTOs.Announcement;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Enums;

namespace TenderManagement.API.Controllers
{
    [ApiController]
    [Route("api/announcements")]
    public class AnnouncementsController : ControllerBase
    {
        private readonly IAnnouncementService _announcementService;

        public AnnouncementsController(IAnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }

        /// <summary>GET /api/announcements/published - public, backs the homepage</summary>
        [HttpGet("published")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublished() => Ok(await _announcementService.GetPublishedAsync());

        /// <summary>GET /api/announcements - Admin only, includes drafts</summary>
        [HttpGet]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> GetAll() => Ok(await _announcementService.GetAllAsync());

        /// <summary>GET /api/announcements/{id} - Admin only</summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> GetById(int id) => Ok(await _announcementService.GetByIdAsync(id));

        /// <summary>POST /api/announcements - Admin only</summary>
        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Create([FromBody] CreateAnnouncementDto dto)
        {
            var result = await _announcementService.CreateAsync(dto, User.GetUserId());
            return Ok(result);
        }

        /// <summary>PUT /api/announcements/{id} - Admin only</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAnnouncementDto dto)
        {
            var result = await _announcementService.UpdateAsync(id, dto, User.GetUserId());
            return Ok(result);
        }

        /// <summary>DELETE /api/announcements/{id} - Admin only, soft delete</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            await _announcementService.DeleteAsync(id);
            return NoContent();
        }
    }
}
