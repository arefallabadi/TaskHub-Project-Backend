namespace TaskHub.API.DTOs.Comment
{
    public class CommentDto
    {
        public int Id { get; set; }

        public string Content { get; set; }

        public int TaskId { get; set; }

        public int UserId { get; set; }

        public string? UserName { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}