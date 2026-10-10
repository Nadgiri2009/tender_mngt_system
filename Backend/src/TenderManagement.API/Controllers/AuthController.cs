using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.Application.DTOs.Auth;
using TenderManagement.Application.Interfaces;

namespace TenderManagement.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>POST /api/auth/login</summary>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }

        /// <summary>POST /api/auth/register</summary>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }
    }
}
