using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskHub.API.DTOs.Pagination;
using TaskHub.API.DTOs.User;
using TaskHub.API.Services.Interfaces;

namespace TaskHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        }

        // GET: api/users?pageNumber=1&pageSize=10
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAll([FromQuery] PaginationParams pagination)
        {
            var users = _userService.GetAll(pagination);
            return Ok(users);
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var currentUserId = GetUserId();
            var currentUserRole = GetUserRole();

            try
            {
                var user = _userService.GetById(id, currentUserId, currentUserRole);
                if (user == null)
                    return NotFound();
                return Ok(user);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult Create([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _userService.Create(dto);
            return Ok(new { Message = "User created successfully" });
        }

        // PUT: api/users/5
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = GetUserId();
            var currentUserRole = GetUserRole();

            try
            {
                var updatedUser = _userService.Update(id, dto, currentUserId, currentUserRole);
                if (updatedUser == false)
                    return NotFound();
                return Ok(new { Message = "User updated successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // DELETE: api/users/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var deleted = _userService.Delete(id);
            if (!deleted)
                return NotFound();
            return NoContent();
        }
    }
}
