using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Dino.Core.AdminBL;
using ForumSimpleAdmin.BL.Cache;
using ForumSimpleAdmin.BL.Contracts;
using ForumSimpleAdmin.BL.Data;
using ForumSimpleAdmin.BL.Forum;
using ForumSimpleAdmin.BL.Models;
using Microsoft.EntityFrameworkCore;
using ForumEntity = ForumSimpleAdmin.BL.Models.Forum;

namespace ForumSimpleAdmin.BL.BL
{
    public class ForumBL : BaseBL<MainDbContext, BlConfig, DinoCacheManager>
    {
        private const int SessionDurationDays = 30;
        private const int CachedPostsTake = 20;

        public ForumBL(BLFactory<MainDbContext, BlConfig, DinoCacheManager> factory, MainDbContext context, IMapper mapper)
            : base(factory, context, mapper)
        {
        }

        public async Task EnsureInitializedAsync()
        {
            bool hasSiteSettings = await Db.SiteSettings.AsNoTracking().AnyAsync();
            if (!hasSiteSettings)
            {
                Db.SiteSettings.Add(new SiteSettings
                {
                    IsLocked = false
                });
            }

            bool hasForums = await Db.Forums.AsNoTracking().AnyAsync();
            if (!hasForums)
            {
                Db.Forums.AddRange(
                    new ForumEntity { Name = "General Discussion", SortIndex = 1, ManagersOnlyPosting = false, Active = true },
                    new ForumEntity { Name = "Announcements", SortIndex = 2, ManagersOnlyPosting = true, Active = true },
                    new ForumEntity { Name = "Support", SortIndex = 3, ManagersOnlyPosting = false, Active = true });
            }

            await Db.SaveChangesAsync();
        }

        public async Task<bool> IsSiteLockedAsync()
        {
            SiteSettings? settings = await Db.SiteSettings.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync();
            bool isLocked = settings != null && settings.IsLocked;
            return isLocked;
        }

        public async Task<List<ForumLookupDto>> GetForumsAsync()
        {
            List<ForumLookupDto> forums = await Db.Forums
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Active)
                .OrderBy(x => x.SortIndex)
                .ThenBy(x => x.Name)
                .Select(x => new ForumLookupDto
                {
                    ForumId = x.Id,
                    ForumName = x.Name,
                    ManagersOnlyPosting = x.ManagersOnlyPosting
                })
                .ToListAsync();

            return forums;
        }

        public async Task<AuthResultDto?> RegisterUserAsync(string name, string password, bool isManager, string? profilePicturePath)
        {
            AuthResultDto? result = null;

            bool exists = await Db.ForumUsers.AsNoTracking().AnyAsync(x => x.Name == name && !x.IsDeleted);
            if (!exists)
            {
                ForumUser user = new ForumUser
                {
                    Name = name,
                    PasswordHash = HashPassword(password),
                    IsManager = isManager,
                    ProfilePicturePath = profilePicturePath
                };
                Db.ForumUsers.Add(user);
                await Db.SaveChangesAsync();
                result = await CreateSessionAndBuildAuthResultAsync(user);
            }

            return result;
        }

        public async Task<AuthResultDto?> LoginAsync(string name, string password)
        {
            string passwordHash = HashPassword(password);
            ForumUser? user = await Db.ForumUsers.FirstOrDefaultAsync(x =>
                x.Name == name &&
                x.PasswordHash == passwordHash &&
                !x.IsDeleted);

            AuthResultDto? result = null;
            if (user != null)
            {
                result = await CreateSessionAndBuildAuthResultAsync(user);
            }

            return result;
        }

        public async Task<ForumUser?> GetUserByTokenAsync(string token)
        {
            ForumSession? session = await Db.ForumSessions
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == token && x.ExpirationDate > DateTime.UtcNow);

            ForumUser? user = session?.User;
            return user;
        }

        public Task<List<ForumPostPreviewDto>> GetFirstPostsByForumAsync(int forumId, int? take = null)
        {
            int resolvedTake = take ?? CachedPostsTake;
            return Cache.GetFirstPostsByForumAsync(forumId, resolvedTake);
        }

        public async Task<ForumPostDetailsDto?> GetPostAsync(int postId)
        {
            ForumPostDetailsDto? dto = await Db.ForumPosts
                .AsNoTracking()
                .Where(x => x.Id == postId && !x.IsDeleted)
                .Select(x => new ForumPostDetailsDto
                {
                    PostId = x.Id,
                    ForumId = x.ForumId,
                    ForumName = x.Forum != null ? x.Forum.Name : string.Empty,
                    UserId = x.UserId,
                    UserName = x.User != null ? x.User.Name : string.Empty,
                    Title = x.Title,
                    Content = x.Content,
                    CreateDate = x.CreateDate,
                    Comments = x.Comments
                        .Where(c => !c.IsDeleted)
                        .OrderBy(c => c.CreateDate)
                        .Select(c => new ForumCommentDto
                        {
                            CommentId = c.Id,
                            UserId = c.UserId,
                            UserName = c.User != null ? c.User.Name : string.Empty,
                            Content = c.Content,
                            CreateDate = c.CreateDate
                        }).ToList()
                }).FirstOrDefaultAsync();

            return dto;
        }

        public async Task<(bool success, string error, int postId)> CreatePostAsync(int userId, int forumId, string title, string content)
        {
            bool success = false;
            string error = string.Empty;
            int postId = 0;

            ForumEntity? forum = await Db.Forums.FirstOrDefaultAsync(x => x.Id == forumId && !x.IsDeleted && x.Active);
            ForumUser? user = await Db.ForumUsers.FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
            bool canPost = false;

            if (forum == null || user == null)
            {
                error = "Invalid forum or user.";
            }
            else
            {
                canPost = !forum.ManagersOnlyPosting || user.IsManager;
                if (!canPost)
                {
                    error = "Only managers can post in this forum.";
                }
            }

            if (string.IsNullOrWhiteSpace(error))
            {
                ForumPost post = new ForumPost
                {
                    ForumId = forumId,
                    UserId = userId,
                    Title = title,
                    Content = content
                };

                Db.ForumPosts.Add(post);
                await Db.SaveChangesAsync();

                postId = post.Id;
                Cache.InvalidateFirstPostsByForum(forumId, CachedPostsTake);
                success = true;
            }

            return (success, error, postId);
        }

        public async Task<(bool success, string error, int commentId)> AddCommentAsync(int userId, int postId, string content)
        {
            bool success = false;
            string error = string.Empty;
            int commentId = 0;

            ForumPost? post = await Db.ForumPosts.FirstOrDefaultAsync(x => x.Id == postId && !x.IsDeleted);
            ForumUser? user = await Db.ForumUsers.FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
            if (post == null || user == null)
            {
                error = "Invalid post or user.";
            }

            if (string.IsNullOrWhiteSpace(error))
            {
                ForumComment comment = new ForumComment
                {
                    PostId = postId,
                    UserId = userId,
                    Content = content
                };
                Db.ForumComments.Add(comment);
                await Db.SaveChangesAsync();

                commentId = comment.Id;
                Cache.InvalidateFirstPostsByForum(post!.ForumId, CachedPostsTake);
                success = true;
            }

            return (success, error, commentId);
        }

        public async Task<(bool success, string error)> DeleteCommentAsync(int actingUserId, int commentId)
        {
            bool success = false;
            string error = string.Empty;

            ForumComment? comment = await Db.ForumComments
                .Include(x => x.Post)
                .FirstOrDefaultAsync(x => x.Id == commentId && !x.IsDeleted);

            if (comment == null || comment.Post == null || comment.Post.IsDeleted)
            {
                error = "Comment was not found.";
            }
            else
            {
                bool isPostCreator = comment.Post.UserId == actingUserId;
                if (!isPostCreator)
                {
                    error = "Only post creator can delete comments.";
                }
            }

            if (string.IsNullOrWhiteSpace(error))
            {
                comment!.IsDeleted = true;
                comment.UpdateDate = DateTime.UtcNow;
                comment.UpdateBy = actingUserId;
                await Db.SaveChangesAsync();
                Cache.InvalidateFirstPostsByForum(comment.Post!.ForumId, CachedPostsTake);
                success = true;
            }

            return (success, error);
        }

        private async Task<AuthResultDto> CreateSessionAndBuildAuthResultAsync(ForumUser user)
        {
            string token = Guid.NewGuid().ToString("N");
            DateTime expirationDate = DateTime.UtcNow.AddDays(SessionDurationDays);

            ForumSession session = new ForumSession
            {
                UserId = user.Id,
                Token = token,
                ExpirationDate = expirationDate
            };
            Db.ForumSessions.Add(session);
            await Db.SaveChangesAsync();

            AuthResultDto result = new AuthResultDto
            {
                UserId = user.Id,
                UserName = user.Name,
                IsManager = user.IsManager,
                ProfilePicturePath = user.ProfilePicturePath,
                Token = token,
                TokenExpirationDate = expirationDate
            };

            return result;
        }

        private static string HashPassword(string password)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = SHA256.HashData(bytes);
            string hashString = Convert.ToHexString(hash);
            return hashString;
        }
    }
}
