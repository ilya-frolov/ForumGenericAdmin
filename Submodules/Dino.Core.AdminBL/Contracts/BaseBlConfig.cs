using System;

namespace Dino.Core.AdminBL.Contracts
{
    public class BaseBlConfig
    {
        public bool DebugMode { get; set; }
        public StorageConfig StorageConfig { get; set; }
        public CacheConfig CacheConfig { get; set; }
    }

    public class StorageConfig
    {
        public bool UseAzureBlob { get; set; }
        public string AzureBlobConnectionString { get; set; }
        public string AzureBlobContainerName { get; set; }
        public string AzureBlobBaseUrl { get; set; }
    }

    public class CacheConfig
    {
        public bool UseRedis { get; set; } = true;
        public string RedisHost { get; set; }
        public int RedisPort { get; set; } = 6379;
        public string RedisPassword { get; set; }
        public int RedisDatabase { get; set; } = 0;
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
    }
} 