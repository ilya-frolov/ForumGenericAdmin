using ForumSimpleAdmin.Api.Controllers.Base;
using ForumSimpleAdmin.Api.Models;
using ForumSimpleAdmin.BL.BL;
using ForumSimpleAdmin.BL.Forum;
using Microsoft.AspNetCore.Mvc;

namespace ForumSimpleAdmin.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForumController : MainAppBaseController<ForumController>
    {
        private readonly IWebHostEnvironment _environment;

        public ForumController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet("forums")]
        public async Task<JsonResult> GetForums()
        {
            ForumBL forumBl = GetBL<ForumBL>(forceNewContext: true);
            bool isLocked = await forumBl.IsSiteLockedAsync();
            List<ForumLookupDto> forums = await forumBl.GetForumsAsync();

            ForumsResponse response = new ForumsResponse
            {
                Forums = forums,
                IsSiteLocked = isLocked
            };

            JsonResult result = CreateJsonResponse(true, response, null, true);
            return result;
        }

        [HttpPost("register")]
        [Consumes("multipart/form-data")]
        public async Task<JsonResult> Register([FromForm] RegisterRequest request, IFormFile? profilePicture)
        {
            if (!ModelState.IsValid)
            {
                return CreateJsonResponse(false, null, "Invalid registration payload.");
            }

            ForumBL forumBl = GetBL<ForumBL>(forceNewContext: true);
            bool isLocked = await forumBl.IsSiteLockedAsync();
            if (isLocked)
            {
                return CreateJsonResponse(false, null, "Site is locked.");
            }

            string? profilePicturePath = await SaveProfilePictureAsync(profilePicture);
            AuthResultDto? auth = await forumBl.RegisterUserAsync(request.Name.Trim(), request.Password, request.IsManager, profilePicturePath);

            JsonResult result;
            if (auth == null)
            {
                result = CreateJsonResponse(false, null, "User name is already taken.");
            }
            else
            {
                result = CreateJsonResponse(true, new AuthResponse { Auth = auth }, null);
            }

            return result;
        }

        [HttpPost("login")]
        public async Task<JsonResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return CreateJsonResponse(false, null, "Invalid login payload.");
            }

            ForumBL forumBl = GetBL<ForumBL>(forceNewContext: true);
            bool isLocked = await forumBl.IsSiteLockedAsync();
            if (isLocked)
            {
                return CreateJsonResponse(false, null, "Site is locked.");
            }

            AuthResultDto? auth = await forumBl.LoginAsync(request.Name.Trim(), request.Password);
            JsonResult result;
            if (auth == null)
            {
                result = CreateJsonResponse(false, null, "Invalid username or password.");
            }
            else
            {
                result = CreateJsonResponse(true, new AuthResponse { Auth = auth }, null);
            }

            return result;
        }

        [HttpGet("posts/{forumId:int}")]
        public async Task<JsonResult> GetFirstPosts([FromRoute] int forumId, [FromQuery] int take = 20)
        {
            ForumBL forumBl = GetBL<ForumBL>(forceNewContext: true);
            List<ForumPostPreviewDto> posts = await forumBl.GetFirstPostsByForumAsync(forumId, take);
            PostsResponse response = new PostsResponse { Posts = posts };

            JsonResult result = CreateJsonResponse(true, response, null, true);
            return result;
        }

        [HttpGet("post/{postId:int}")]
        public async Task<JsonResult> GetPost([FromRoute] int postId)
        {
            ForumBL forumBl = GetBL<ForumBL>(forceNewContext: true);
            ForumPostDetailsDto? post = await forumBl.GetPostAsync(postId);

            JsonResult result;
            if (post == null)
            {
                result = CreateJsonResponse(false, null, "Post not found.", true);
            }
            else
            {
                result = CreateJsonResponse(true, new PostResponse { Post = post }, null, true);
            }

            return result;
        }

        [HttpPost("post")]
        public async Task<JsonResult> CreatePost([FromBody] CreatePostRequest request)
        {
            if (!ModelState.IsValid)
            {
                return CreateJsonResponse(false, null, "Invalid post payload.");
            }

            ForumBL forumBl = GetBL<ForumBL>(forceNewContext: true);
            bool isLocked = await forumBl.IsSiteLockedAsync();
            if (isLocked)
            {
                return CreateJsonResponse(false, null, "Site is locked.");
            }

            string token = GetForumToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return CreateJsonResponse(false, null, "Missing forum token.");
            }

            var user = await forumBl.GetUserByTokenAsync(token);
            if (user == null)
            {
                return CreateJsonResponse(false, null, "Invalid or expired forum token.");
            }

            var createResult = await forumBl.CreatePostAsync(user.Id, request.ForumId, request.Title.Trim(), request.Content.Trim());
            JsonResult result;
            if (!createResult.success)
            {
                result = CreateJsonResponse(false, null, createResult.error);
            }
            else
            {
                result = CreateJsonResponse(true, new { PostId = createResult.postId }, null);
            }

            return result;
        }

        [HttpPost("comment")]
        public async Task<JsonResult> AddComment([FromBody] CreateCommentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return CreateJsonResponse(false, null, "Invalid comment payload.");
            }

            ForumBL forumBl = GetBL<ForumBL>(forceNewContext: true);
            bool isLocked = await forumBl.IsSiteLockedAsync();
            if (isLocked)
            {
                return CreateJsonResponse(false, null, "Site is locked.");
            }

            string token = GetForumToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return CreateJsonResponse(false, null, "Missing forum token.");
            }

            var user = await forumBl.GetUserByTokenAsync(token);
            if (user == null)
            {
                return CreateJsonResponse(false, null, "Invalid or expired forum token.");
            }

            var addResult = await forumBl.AddCommentAsync(user.Id, request.PostId, request.Content.Trim());
            JsonResult result;
            if (!addResult.success)
            {
                result = CreateJsonResponse(false, null, addResult.error);
            }
            else
            {
                result = CreateJsonResponse(true, new { CommentId = addResult.commentId }, null);
            }

            return result;
        }

        [HttpDelete("comment")]
        public async Task<JsonResult> DeleteComment([FromBody] DeleteCommentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return CreateJsonResponse(false, null, "Invalid delete comment payload.");
            }

            string token = GetForumToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return CreateJsonResponse(false, null, "Missing forum token.");
            }

            ForumBL forumBl = GetBL<ForumBL>(forceNewContext: true);
            var user = await forumBl.GetUserByTokenAsync(token);
            if (user == null)
            {
                return CreateJsonResponse(false, null, "Invalid or expired forum token.");
            }

            var deleteResult = await forumBl.DeleteCommentAsync(user.Id, request.CommentId);
            JsonResult result;
            if (!deleteResult.success)
            {
                result = CreateJsonResponse(false, null, deleteResult.error);
            }
            else
            {
                result = CreateJsonResponse(true, true, null);
            }

            return result;
        }

        private async Task<string?> SaveProfilePictureAsync(IFormFile? profilePicture)
        {
            string? relativePath = null;
            if (profilePicture != null && profilePicture.Length > 0)
            {
                string webRoot = _environment.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRoot))
                {
                    webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
                }

                string profilesFolder = Path.Combine(webRoot, "uploads", "profiles");
                if (!Directory.Exists(profilesFolder))
                {
                    Directory.CreateDirectory(profilesFolder);
                }

                string extension = Path.GetExtension(profilePicture.FileName);
                string safeExtension = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension;
                string fileName = $"{Guid.NewGuid():N}{safeExtension}";
                string fullPath = Path.Combine(profilesFolder, fileName);

                await using (FileStream stream = new FileStream(fullPath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                relativePath = Path.Combine("uploads", "profiles", fileName).Replace("\\", "/");
            }

            return relativePath;
        }

        private string GetForumToken()
        {
            string token = string.Empty;
            bool hasToken = Request.Headers.TryGetValue("X-Forum-Token", out var tokenValue);
            if (hasToken)
            {
                token = tokenValue.ToString();
            }

            return token;
        }
    }
}
