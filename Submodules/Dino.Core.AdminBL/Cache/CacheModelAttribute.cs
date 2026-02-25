using System;

namespace Dino.CoreMvc.Admin.Models.Admin // TODO: Consider moving this namespace if it's no longer Admin specific
{
    /// <summary>
    /// Attribute to define caching behavior for data models.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CacheModelAttribute : Attribute 
    {
        /// <summary>
        /// Time, in seconds, for cache expiration. If not set, uses default expiration.
        /// </summary>
        public int Expiration { get; set; }

        /// <summary>
        /// Whether to use sliding expiration (resets timer on access) or absolute expiration.
        /// </summary>
        public bool UseSlidingExpiration { get; set; }

        /// <summary>
        /// The type of database model to map from. Required for proper caching initialization.
        /// </summary>
        public Type DbModelType { get; set; }

        /// <summary>
        /// When true, automatic AutoMapper configuration (DbModel -> CacheModel) is skipped.
        /// The mapping must be configured manually elsewhere (for example, in the BLAutoMapperProfile).
        /// </summary>
        public bool ManualMapping { get; set; } = false;

        /// <summary>
        /// When to populate the cache.
        /// </summary>
        public CacheTiming CacheTiming { get; set; } = CacheTiming.OnFirstAccess;

        /// <summary>
        /// Whether to cache the model when it's created (via related AdminModel).
        /// This might need context from the specific AdminModel. Consider if this setting makes sense here.
        /// Let's keep it for now, but its application might change.
        /// </summary>
        public bool CacheOnCreate { get; set; } = true;

        /// <summary>
        /// Whether to update the cache when the related AdminModel is edited.
        /// </summary>
        public bool UpdateOnEdit { get; set; } = true;

        /// <summary>
        /// Whether to remove the model from cache when the related AdminModel is deleted.
        /// </summary>
        public bool RemoveOnDelete { get; set; } = true;

        /// <summary>
        /// Whether to reload the cache when the sort order changes (if applicable to related AdminModel).
        /// </summary>
        public bool ReloadOnSort { get; set; } = true;

        /// <summary>
        /// Whether to use Redis for caching.
        /// </summary>
        public bool UseRedis { get; set; } = true; // Note: Actual Redis usage is often configured globally or per cache instance.
    }

    /// <summary>
    /// Specifies when to populate the cache.
    /// </summary>
    public enum CacheTiming
    {
        /// <summary>
        /// Cache is populated when the model is first accessed (e.g., via GetOrCreate).
        /// </summary>
        OnFirstAccess,

        /// <summary>
        /// Cache is populated when the application starts. Requires DbModelType to be set.
        /// </summary>
        OnApplicationStart
    }
} 