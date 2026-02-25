using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Dino.Common.Hangfire.Jobs
{
    /// <summary>
    /// Base abstract class for Hangfire jobs
    /// </summary>
    public abstract class BaseHangfireJob : IHangfireJob
    {
        protected readonly ILogger _logger;

        protected BaseHangfireJob(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the job name, which should be unique
        /// </summary>
        public abstract string JobName { get; }

        /// <summary>
        /// Gets the CRON schedule expression for recurring jobs
        /// </summary>
        public abstract string CronSchedule { get; }

        /// <summary>
        /// Gets the queue name for the job
        /// </summary>
        public virtual string Queue => "default";

        /// <summary>
        /// Executes the job with error handling
        /// </summary>
        public async Task ExecuteAsync()
        {
            try
            {
                _logger.LogInformation("Starting job: {JobName}", JobName);
                
                var startTime = DateTime.UtcNow;
                await ExecuteJobAsync();
                var duration = DateTime.UtcNow - startTime;
                
                _logger.LogInformation("Job {JobName} completed in {Duration}ms", JobName, duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing job {JobName}: {ErrorMessage}", JobName, ex.Message);
                throw; // Rethrow so Hangfire marks the job as failed
            }
        }

        /// <summary>
        /// The actual job implementation to be provided by derived classes
        /// </summary>
        protected abstract Task ExecuteJobAsync();
    }
} 