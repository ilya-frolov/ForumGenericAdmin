namespace ForumSimpleAdmin.BL.Models
{
    public class ForumUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? ProfilePicturePath { get; set; }
        public bool IsManager { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
        public int UpdateBy { get; set; } = 1;

        public virtual ICollection<ForumPost> Posts { get; set; } = new List<ForumPost>();
        public virtual ICollection<ForumComment> Comments { get; set; } = new List<ForumComment>();
    }

    public class Forum
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool ManagersOnlyPosting { get; set; }
        public bool Active { get; set; } = true;
        public int SortIndex { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
        public int UpdateBy { get; set; } = 1;

        public virtual ICollection<ForumPost> Posts { get; set; } = new List<ForumPost>();
    }

    public class ForumPost
    {
        public int Id { get; set; }
        public int ForumId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
        public int UpdateBy { get; set; } = 1;

        public virtual Forum? Forum { get; set; }
        public virtual ForumUser? User { get; set; }
        public virtual ICollection<ForumComment> Comments { get; set; } = new List<ForumComment>();
    }

    public class ForumComment
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
        public int UpdateBy { get; set; } = 1;

        public virtual ForumPost? Post { get; set; }
        public virtual ForumUser? User { get; set; }
    }

    public class ForumSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        public virtual ForumUser? User { get; set; }
    }

    public class SiteSettings
    {
        public int Id { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
        public int UpdateBy { get; set; } = 1;
    }
}
