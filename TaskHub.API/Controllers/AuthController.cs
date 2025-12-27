using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using TaskHub.API.DTOs.Auth;
using TaskHub.API.Services.Interfaces;

namespace TaskHub.API.Controllers
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

        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { Message = "Username and password are required" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors.Select(e => e.ErrorMessage))
                    .ToList();
                
                return BadRequest(new { 
                    Message = errors.FirstOrDefault() ?? "Invalid input data",
                    Errors = errors 
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { Message = "Username and password are required" });
            }

            var result = _authService.Login(dto);
            if (result == null)
            {
                return Unauthorized(new { Message = "Invalid username or password" });
            }

            return Ok(result);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public IActionResult Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = _authService.Register(dto);
            if (result == null)
                return BadRequest(new { Message = "Username already exists or registration failed" });

            return Ok(result);
        }

        [HttpPost("token")]
        [AllowAnonymous]
        public IActionResult SystemAuth([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_authService.ValidateSystemCredentials(dto.Username, dto.Password))
                return Unauthorized(new { Message = "Invalid system credentials" });

            var token = _authService.GenerateToken(dto.Username, "Admin", 0);
            if (token == null)
                return StatusCode(500, new { Message = "Failed to generate token" });

            return Ok(new { token });
        }
    }
}
