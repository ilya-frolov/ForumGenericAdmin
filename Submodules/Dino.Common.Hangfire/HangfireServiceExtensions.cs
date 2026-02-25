using Dino.Common.Hangfire.Configuration;
using Dino.Common.Hangfire.Jobs;
using Dino.Common.Hangfire.Security;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Dino.Common.Hangfire
{
    /// <summary>
    /// Extension methods for configuring Hangfire services
    /// </summary>
    public static class HangfireServiceExtensions
    {
        /// <summary>
        /// Adds and configures Hangfire services
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="configSectionName">The configuration section name (default: "Hangfire")</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddDinoHangfire(
            this IServiceCollection services,
            IConfiguration configuration,
            string configSectionName = "Hangfire")
        {
            // Bind configuration
            var hangfireConfig = new HangfireConfig();
            configuration.GetSection(configSectionName).Bind(hangfireConfig);
            services.Configure<HangfireConfig>(configuration.GetSection(configSectionName));

            // Validate connection string
            if (string.IsNullOrEmpty(hangfireConfig.ConnectionString))
            {
                // If no connection string, register dependencies but don't configure Hangfire
                services.AddSingleton<JobScheduler>();
                return services;
            }

            // Add Hangfire services
            services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(GetCompatibilityLevel(hangfireConfig.CompatibilityLevel))
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(hangfireConfig.ConnectionString, new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.FromSeconds(15),
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true,
                        PrepareSchemaIfNecessary = hangfireConfig.CreateDatabaseTablesIfNotExist
                    });
            });

            // Register authorization filter
            services.AddSingleton<HangfireAuthorizationFilter>();

            // Register scheduler and recurring job manager as singletons
            services.AddSingleton<IRecurringJobManager, RecurringJobManager>();
            services.AddSingleton<JobScheduler>();

            // Add Hangfire server if processing is enabled
            if (hangfireConfig.EnableProcessing)
            {
                services.AddHangfireServer(options =>
                {
                    if (hangfireConfig.Queues != null && hangfireConfig.Queues.Any())
                    {
                        options.Queues = hangfireConfig.Queues;
                    }
                });
            }

            return services;
        }

        /// <summary>
        /// Configures the Hangfire dashboard and initializes jobs
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <param name="initializeJobs">Whether to initialize jobs on startup (default: true)</param>
        /// <returns>The application builder for chaining</returns>
        public static IApplicationBuilder UseDinoHangfire(
            this IApplicationBuilder app,
            bool initializeJobs = true)
        {
            // Get configuration
            var config = app.ApplicationServices.GetService<Microsoft.Extensions.Options.IOptions<HangfireConfig>>()?.Value;
            if (config == null || string.IsNullOrEmpty(config.ConnectionString))
            {
                return app;
            }

            var logger = app.ApplicationServices.GetService<ILogger<JobScheduler>>();

            // Use Hangfire dashboard if enabled
            if (config.EnableDashboard)
            {
                app.UseHangfireDashboard(
                    config.DashboardPath,
                    new DashboardOptions
                    {
                        Authorization = new[] 
                        { 
                            app.ApplicationServices.GetRequiredService<HangfireAuthorizationFilter>() 
                        }
                    });
                
                logger?.LogInformation("Hangfire dashboard enabled at {Path}", config.DashboardPath);
            }
            else
            {
                logger?.LogInformation("Hangfire dashboard is disabled by configuration");
            }

            // Initialize jobs if requested
            if (initializeJobs)
            {
                var jobScheduler = app.ApplicationServices.GetService<JobScheduler>();
                jobScheduler?.RegisterAllJobs();
            }

            return app;
        }

        /// <summary>
        /// Gets the compatibility level from an integer
        /// </summary>
        private static CompatibilityLevel GetCompatibilityLevel(int level)
        {
            return level switch
            {
                110 => CompatibilityLevel.Version_110,
                170 => CompatibilityLevel.Version_170,
                180 => CompatibilityLevel.Version_180,
                _ => CompatibilityLevel.Version_180
            };
        }
    }
} 