using System.Security;
using TaskHub.API.DTOs.Pagination;
using TaskHub.API.DTOs.Task;
using TaskHub.API.Entities;
using TaskHub.API.Enums;
using TaskHub.API.Repositories;
using TaskHub.API.Services.Interfaces;

namespace TaskHub.API.Services.Implementations
{
    public class TaskService : ITaskService
    {
        private readonly IRepository<TaskItem> _taskRepository;
        private readonly IRepository<User> _userRepository;

        public TaskService(IRepository<TaskItem> taskRepository, IRepository<User> userRepository)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
        }

        public List<TaskDto> GetAll(PaginationParams pagination, int userId, string userRole)
        {
            var allTasks = _taskRepository.GetAll();

            if (userRole != "Admin")
            {
                allTasks = allTasks.Where(t => t.AssignedUserId == userId).ToList();
            }

            var tasks = allTasks
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList();

            var userIds = tasks.Where(t => t.AssignedUserId.HasValue)
                .Select(t => t.AssignedUserId.Value)
                .Distinct()
                .ToList();

            var users = _userRepository.GetAll()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.Username);

            return tasks.Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                AssignedUserId = t.AssignedUserId ?? 0,
                AssignedUser = t.AssignedUserId.HasValue && users.ContainsKey(t.AssignedUserId.Value)
                    ? users[t.AssignedUserId.Value]
                    : null
            }).ToList();
        }

        public TaskDto? GetById(int id, int userId, string userRole)
        {
            var task = _taskRepository.GetById(id);
            if (task == null) return null;

            if (userRole != "Admin" && task.AssignedUserId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to view this task.");
            }

            string? assignedUsername = null;
            if (task.AssignedUserId.HasValue)
            {
                var user = _userRepository.GetById(task.AssignedUserId.Value);
                assignedUsername = user?.Username;
            }

            return new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                AssignedUserId = task.AssignedUserId ?? 0,
                AssignedUser = assignedUsername
            };
        }

        public void Create(CreateTaskDto dto)
        {
            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                Status = dto.Status, 
                AssignedUserId = dto.AssignedUserId
            };

            _taskRepository.Add(task);
        }

        public void Update(int id, UpdateTaskDto dto, int userId, string userRole)
        {
            var task = _taskRepository.GetById(id);
            if (task == null) 
                throw new System.ArgumentException("Task not found.");

            // Check if user has permission to update this task
            if (userRole != "Admin" && task.AssignedUserId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to update this task.");
            }

            // All users (including regular users) can update Title and Description
            if (!string.IsNullOrEmpty(dto.Title))
                task.Title = dto.Title;

            if (!string.IsNullOrEmpty(dto.Description))
                task.Description = dto.Description;

            // Only Admins can update Status
            if (dto.Status != default(TaskEnum))
            {
                if (userRole != "Admin")
                {
                    throw new UnauthorizedAccessException("Only Admin can update task status. Regular users can only update title and description.");
                }
                task.Status = dto.Status;
            }

            // Only Admins can update AssignedUserId
            if (dto.AssignedUserId.HasValue)
            {
                if (userRole != "Admin")
                {
                    throw new UnauthorizedAccessException("Only Admin can change task assignee. Regular users can only update title and description.");
                }

                var assignedUser = _userRepository.GetById(dto.AssignedUserId.Value);
                if (assignedUser == null)
                {
                    throw new System.ArgumentException("Assigned user not found.");
                }

                task.AssignedUserId = dto.AssignedUserId.Value;
            }

            _taskRepository.Update(task);
        }

        public bool Delete(int id, int userId, string userRole)
        {
            var task = _taskRepository.GetById(id);
            if (task == null) return false;

            if (userRole != "Admin" && task.AssignedUserId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this task.");
            }

            _taskRepository.Delete(task);
            return true;
        }

        public void ChangeStatus(int id, TaskEnum status, int userId, string userRole)
        {
            var task = _taskRepository.GetById(id);
            if (task == null)
                throw new Exception("Task not found");

            if (userRole != "Admin" && task.AssignedUserId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to change the status of this task.");
            }

            task.Status = status;
            _taskRepository.Update(task);
        }
    }
}
