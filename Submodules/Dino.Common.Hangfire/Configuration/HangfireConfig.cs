using System.Collections.Generic;

namespace Dino.Common.Hangfire.Configuration
{
    /// <summary>
    /// Configuration settings for Hangfire
    /// </summary>
    public class HangfireConfig
    {
        /// <summary>
        /// Connection string to the Hangfire database
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// List of IP addresses allowed to access the Hangfire dashboard
        /// </summary>
        public List<string> DashboardAllowedIps { get; set; } = new List<string>();

        /// <summary>
        /// Whether to enable Hangfire server processing
        /// </summary>
        public bool EnableProcessing { get; set; } = true;

        /// <summary>
        /// Whether to expose the Hangfire dashboard
        /// </summary>
        public bool EnableDashboard { get; set; } = true;

        /// <summary>
        /// Dashboard route path (default: /hangfire)
        /// </summary>
        public string DashboardPath { get; set; } = "/hangfire";

        /// <summary>
        /// Job queues to process (default: "default")
        /// </summary>
        public string[] Queues { get; set; } = new[] { "default" };

        /// <summary>
        /// Compatibility level for Hangfire
        /// </summary>
        public int CompatibilityLevel { get; set; } = 180;

        /// <summary>
        /// Whether to automatically create database tables if they don't exist
        /// </summary>
        public bool CreateDatabaseTablesIfNotExist { get; set; } = true;
    }
} 