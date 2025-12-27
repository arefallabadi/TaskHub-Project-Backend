using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskHub.API.DTOs.Pagination;
using TaskHub.API.DTOs.Task;
using TaskHub.API.Enums;
using TaskHub.API.Services.Interfaces;

namespace TaskHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
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

        // GET: api/tasks?pageNumber=1&pageSize=10
        [HttpGet]
        public IActionResult GetAll([FromQuery] PaginationParams pagination)
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            var tasks = _taskService.GetAll(pagination, userId, userRole);
            return Ok(tasks);
        }

        // GET: api/tasks/5
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            try
            {
                var task = _taskService.GetById(id, userId, userRole);
                if (task == null)
                    return NotFound();
                return Ok(task);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        // POST: api/tasks
        [HttpPost]
        public IActionResult Create([FromBody] CreateTaskDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _taskService.Create(dto);
            return Ok(new { Message = "Task created successfully" });
        }

        // PUT: api/tasks/5
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] UpdateTaskDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var userRole = GetUserRole();

            try
            {
                _taskService.Update(id, dto, userId, userRole);
                return Ok(new { Message = "Task updated successfully" });
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

        // DELETE: api/tasks/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var userId = GetUserId();
            var userRole = GetUserRole();

            try
            {
                var deleted = _taskService.Delete(id, userId, userRole);
                if (!deleted)
                    return NotFound();
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
        }

        // PATCH: api/tasks/5/status
        [HttpPatch("{id}/status")]
        public IActionResult ChangeStatus(int id, [FromBody] TaskEnum status)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var userRole = GetUserRole();

            try
            {
                _taskService.ChangeStatus(id, status, userId, userRole);
                return Ok(new { Message = "Task status updated successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
