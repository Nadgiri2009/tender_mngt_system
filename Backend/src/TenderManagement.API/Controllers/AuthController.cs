using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.API.Extensions;
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

        /// <summary>POST /api/auth/request-otp</summary>
        [AllowAnonymous]
        [HttpPost("request-otp")]
        [ProducesResponseType(typeof(RequestLoginOtpResponseDto), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> RequestOtp(
            [FromBody] RequestLoginOtpRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _authService.RequestLoginOtpAsync(request, cancellationToken);
            return Accepted(result);
        }

        /// <summary>POST /api/auth/login verifies a one-time mobile code.</summary>
        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            return Ok(result);
        }

        /// <summary>POST /api/auth/register</summary>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.RegisterAsync(request, cancellationToken);
            return Ok(result);
        }

        /// <summary>GET /api/auth/profile returns the authenticated user's profile.</summary>
        [Authorize]
        [HttpGet("profile")]
        [ProducesResponseType(typeof(UserProfileResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        {
            var profile = await _authService.GetProfileAsync(User.GetUserId(), cancellationToken);
            return profile is null ? NotFound() : Ok(profile);
        }

        /// <summary>PUT /api/auth/profile updates the authenticated user's name and email.</summary>
        [Authorize]
        [HttpPut("profile")]
        [ProducesResponseType(typeof(UserProfileResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateUserProfileRequestDto request,
            CancellationToken cancellationToken)
        {
            var profile = await _authService.UpdateProfileAsync(
                User.GetUserId(), request, cancellationToken);
            return Ok(profile);
        }
    }
}
