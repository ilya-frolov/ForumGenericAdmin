using System.ComponentModel.DataAnnotations;

namespace ForumSimpleAdmin.Api.Models
{
    public class RegisterRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MinLength(4)]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        public bool IsManager { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;
    }

    public class CreatePostRequest
    {
        [Required]
        public int ForumId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(6000)]
        public string Content { get; set; } = string.Empty;
    }

    public class CreateCommentRequest
    {
        [Required]
        public int PostId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;
    }

    public class DeleteCommentRequest
    {
        [Required]
        public int CommentId { get; set; }
    }
}
