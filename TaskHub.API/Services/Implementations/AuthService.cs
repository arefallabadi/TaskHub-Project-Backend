using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskHub.API.Data;
using TaskHub.API.DTOs.Auth;
using TaskHub.API.Services.Interfaces;

namespace TaskHub.API.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly TaskHubDbContext _context;

        public AuthService(IConfiguration configuration, TaskHubDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public bool ValidateSystemCredentials(string username, string password)
        {
            var sysUser = _configuration["SystemAuth:Username"];
            var sysPass = _configuration["SystemAuth:Password"];

            return username == sysUser && password == sysPass;
        }

        public string? GenerateToken(string username, string role, int userId)
        {
            var key = _configuration["JwtKey"] ?? "ThisIsASecretKeyForJWTToken123!ThisIsASecretKeyForJWTToken123!";
            if (string.IsNullOrEmpty(key)) return null;

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public AuthResponseDto? Login(LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return null;

            var user = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Username == dto.Username);

            if (user == null)
                return null;

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                return null;

            try
            {
                var isValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
                if (!isValid)
                    return null;
            }
            catch (Exception ex)
            {
                return null;
            }

            var token = GenerateToken(user.Username, user.Role.Name, user.Id);
            if (token == null) return null;

            return new AuthResponseDto
            {
                Token = token,
                Role = user.Role.Name
            };
        }

        public AuthResponseDto? Register(RegisterDto dto)
        {
            if (_context.Users.Any(u => u.Username == dto.Username))
                return null;

            Entities.Role? role = null;
            var existingUserWithUserRole = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Role.Name == "User");
            
            if (existingUserWithUserRole != null && existingUserWithUserRole.Role != null)
            {
                role = existingUserWithUserRole.Role;
            }
            else
            {
                role = _context.Set<Entities.Role>()
                    .FirstOrDefault(r => r.Name == "User");
                
                if (role == null)
                {
                    role = new Entities.Role { Name = "User" };
                    _context.Set<Entities.Role>().Add(role);
                    _context.SaveChanges();
                }
            }

            if (string.IsNullOrWhiteSpace(dto.Password))
                return null;

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new Entities.User
            {
                Username = dto.Username,
                Name = dto.Name,
                PasswordHash = passwordHash,
                RoleId = role.Id
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            _context.Entry(user).Reference(u => u.Role).Load();
            var roleName = user.Role?.Name ?? "User";

            var token = GenerateToken(user.Username, roleName, user.Id);
            if (token == null) return null;

            return new AuthResponseDto
            {
                Token = token,
                Role = roleName
            };
        }
    }
}
