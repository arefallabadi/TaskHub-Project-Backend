using Microsoft.EntityFrameworkCore;
using TaskHub.API.Data;
using TaskHub.API.DTOs.Dashboard;
using TaskHub.API.Enums;
using TaskHub.API.Services.Interfaces;

namespace TaskHub.API.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly TaskHubDbContext _context;

        public DashboardService(TaskHubDbContext context)
        {
            _context = context;
        }

        public DashboardDto GetAdminDashboard()
        {
            var totalTasks = _context.Tasks.Count();
            var completedTasks = _context.Tasks.Count(t => t.Status == TaskEnum.Completed);
            var pendingTasks = _context.Tasks.Count(t => t.Status == TaskEnum.ToDo);
            var inProgressTasks = _context.Tasks.Count(t => t.Status == TaskEnum.InProgress);
            var cancelledTasks = _context.Tasks.Count(t => t.Status == TaskEnum.Cancelled);
            var totalUsers = _context.Users.Count();

            return new DashboardDto
            {
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                InProgressTasks = inProgressTasks,
                CancelledTasks = cancelledTasks,
                TotalUsers = totalUsers
            };
        }

        public DashboardDto GetUserDashboard(int userId)
        {
            var userTasks = _context.Tasks.Where(t => t.AssignedUserId == userId);
            var totalTasks = userTasks.Count();
            var completedTasks = userTasks.Count(t => t.Status == TaskEnum.Completed);
            var pendingTasks = userTasks.Count(t => t.Status == TaskEnum.ToDo);
            var inProgressTasks = userTasks.Count(t => t.Status == TaskEnum.InProgress);
            var cancelledTasks = userTasks.Count(t => t.Status == TaskEnum.Cancelled);

            return new DashboardDto
            {
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                InProgressTasks = inProgressTasks,
                CancelledTasks = cancelledTasks,
                TotalUsers = null
            };
        }
    }
}

