using Microsoft.AspNetCore.Mvc;
using TaskHub.API.Data;
using TaskHub.API.Entities;

namespace TaskHub.API.Controllers
{
    public class CreateTestUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
    }

    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        private readonly TaskHubDbContext _context;

        public TestController(TaskHubDbContext context)
        {
            _context = context;
        }

        [HttpPost("create-user")]
        public IActionResult CreateUser([FromBody] CreateTestUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { Message = "Username, Password, and Name are required" });
            }

            if (dto.Role != "Admin" && dto.Role != "User")
            {
                return BadRequest(new { Message = "Role must be 'Admin' or 'User'" });
            }

            var role = _context.Set<Role>().FirstOrDefault(r => r.Name == dto.Role);
            if (role == null)
            {
                role = new Role { Name = dto.Role };
                _context.Set<Role>().Add(role);
                _context.SaveChanges();
            }

            var existingUser = _context.Users.FirstOrDefault(u => u.Username == dto.Username);
            if (existingUser != null)
            {
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                existingUser.Name = dto.Name;
                existingUser.RoleId = role.Id;
                _context.SaveChanges();
                return Ok(new
                {
                    Message = $"User '{dto.Username}' updated successfully",
                    Username = dto.Username,
                    Name = dto.Name,
                    Role = dto.Role,
                    Status = "updated"
                });
            }
            else
            {
                var user = new User
                {
                    Username = dto.Username,
                    Name = dto.Name,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    RoleId = role.Id
                };
                _context.Users.Add(user);
                _context.SaveChanges();
                return Ok(new
                {
                    Message = $"User '{dto.Username}' created successfully",
                    Username = dto.Username,
                    Name = dto.Name,
                    Role = dto.Role,
                    Status = "created"
                });
            }
        }

        [HttpGet("test-password")]
        public IActionResult TestPassword([FromQuery] string password = "aref12345")
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            var verify = BCrypt.Net.BCrypt.Verify(password, hash);
            return Ok(new { Password = password, Hash = hash, Verify = verify });
        }
    }
}
