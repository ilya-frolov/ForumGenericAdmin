using ForumSimpleAdmin.BL.Forum;

namespace ForumSimpleAdmin.Api.Models
{
    public class AuthResponse
    {
        public AuthResultDto Auth { get; set; } = new AuthResultDto();
    }

    public class ForumsResponse
    {
        public List<ForumLookupDto> Forums { get; set; } = new List<ForumLookupDto>();
        public bool IsSiteLocked { get; set; }
    }

    public class PostsResponse
    {
        public List<ForumPostPreviewDto> Posts { get; set; } = new List<ForumPostPreviewDto>();
    }

    public class PostResponse
    {
        public ForumPostDetailsDto? Post { get; set; }
    }
}
