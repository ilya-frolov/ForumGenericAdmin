namespace ForumSimpleAdmin.BL.Forum
{
    public class ForumLookupDto
    {
        public int ForumId { get; set; }
        public string ForumName { get; set; } = string.Empty;
        public bool ManagersOnlyPosting { get; set; }
    }

    public class AuthResultDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public bool IsManager { get; set; }
        public string? ProfilePicturePath { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime TokenExpirationDate { get; set; }
    }

    public class ForumPostPreviewDto
    {
        public int PostId { get; set; }
        public int ForumId { get; set; }
        public string ForumName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int CommentsCount { get; set; }
        public DateTime CreateDate { get; set; }
    }

    public class ForumCommentDto
    {
        public int CommentId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
    }

    public class ForumPostDetailsDto
    {
        public int PostId { get; set; }
        public int ForumId { get; set; }
        public string ForumName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public List<ForumCommentDto> Comments { get; set; } = new List<ForumCommentDto>();
    }
}
