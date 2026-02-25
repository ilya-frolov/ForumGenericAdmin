using AutoMapper;
using Dino.Core.AdminBL;
using Dino.Core.AdminBL.Cache;
using ForumSimpleAdmin.BL.Contracts;
using ForumSimpleAdmin.BL.Data;
using ForumSimpleAdmin.BL.Forum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ForumSimpleAdmin.BL.Cache
{
    public class DinoCacheManager : BaseDinoCacheManager<MainDbContext, BlConfig, DinoCacheManager>
    {
        private static readonly MemoryCacheEntryOptions FirstPostsCacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
        };

        public DinoCacheManager(IConfiguration config, IMapper mapper, IOptions<BlConfig> blConfig, IServiceProvider serviceProvider)
            : base(config, mapper, blConfig, serviceProvider)
        {
        }

        public Task<List<ForumPostPreviewDto>> GetFirstPostsByForumAsync(int forumId, int take)
        {
            return _cacheManager.GetOrCreateByKeyOnlyAsync(
                GetFirstPostsCacheKey(forumId, take),
                async _ =>
                {
                    var db = GetNewDbContext();
                    var query = db.ForumPosts
                        .AsNoTracking()
                        .Where(x => !x.IsDeleted && x.ForumId == forumId)
                        .OrderByDescending(x => x.CreateDate)
                        .Take(take)
                        .Select(x => new ForumPostPreviewDto
                        {
                            PostId = x.Id,
                            ForumId = x.ForumId,
                            ForumName = x.Forum != null ? x.Forum.Name : string.Empty,
                            UserId = x.UserId,
                            UserName = x.User != null ? x.User.Name : string.Empty,
                            Title = x.Title,
                            Content = x.Content,
                            CommentsCount = x.Comments.Count(c => !c.IsDeleted),
                            CreateDate = x.CreateDate
                        });

                    return await query.ToListAsync();
                },
                FirstPostsCacheEntryOptions);
        }

        public void InvalidateFirstPostsByForum(int forumId, int take)
        {
            _cacheManager.RemoveByKeyOnly(GetFirstPostsCacheKey(forumId, take));
        }

        private static string GetFirstPostsCacheKey(int forumId, int take)
        {
            return $"ForumPosts:First:{forumId}:{take}";
        }
    }
}
