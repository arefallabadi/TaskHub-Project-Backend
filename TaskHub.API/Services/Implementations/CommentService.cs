using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Microsoft.EntityFrameworkCore;
using TaskHub.API.Data;
using TaskHub.API.DTOs.Comment;
using TaskHub.API.DTOs.Pagination;
using TaskHub.API.Entities;
using TaskHub.API.Repositories;
using TaskHub.API.Services.Interfaces;

namespace TaskHub.API.Services.Implementations
{
    public class CommentService : ICommentService
    {
        private readonly IRepository<Comment> _commentRepository;
        private readonly TaskHubDbContext _context;

        public CommentService(IRepository<Comment> commentRepository, TaskHubDbContext context)
        {
            _commentRepository = commentRepository;
            _context = context;
        }

        public List<CommentDto> GetAll(PaginationParams pagination, int? taskId, int userId, string userRole)
        {
            var query = _context.Comments.Include(c => c.User).Include(c => c.Task).AsQueryable();

            if (taskId.HasValue)
            {
                query = query.Where(c => c.TaskItemId == taskId.Value);
            }

            if (userRole != "Admin")
            {
                query = query.Where(c => c.Task != null && c.Task.AssignedUserId == userId);
            }

            return query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    TaskId = c.TaskItemId,
                    UserId = c.AuthorId,
                    UserName = c.User != null ? c.User.Username : null,
                    CreatedAt = c.CreatedAt
                })
                .ToList();
        }

        public void Create(CommentDto dto, int userId)
        {
            var comment = new Comment
            {
                Content = dto.Content,
                TaskItemId = dto.TaskId,
                AuthorId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _commentRepository.Add(comment);
        }

        public bool Delete(int id, int userId, string userRole)
        {
            var comment = _context.Comments.Include(c => c.User).FirstOrDefault(c => c.Id == id);
            if (comment == null) return false;

            if (userRole != "Admin" && comment.AuthorId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this comment.");
            }

            _commentRepository.Delete(comment);
            return true;
        }
    }
}
