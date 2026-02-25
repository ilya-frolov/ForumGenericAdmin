using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Dino.Common.Hangfire.Jobs
{
    /// <summary>
    /// Sample job to demonstrate job implementation
    /// </summary>
    public class SampleJob : BaseHangfireJob
    {
        public SampleJob(ILogger<SampleJob> logger) : base(logger)
        {
        }

        /// <summary>
        /// Gets the unique job name
        /// </summary>
        public override string JobName => "SampleJob";

        /// <summary>
        /// Gets the CRON schedule (every hour)
        /// </summary>
        public override string CronSchedule => "0 * * * *";

        /// <summary>
        /// Gets the queue name (default queue)
        /// </summary>
        public override string Queue => "default";

        /// <summary>
        /// Implements the job's actual logic
        /// </summary>
        protected override Task ExecuteJobAsync()
        {
            _logger.LogInformation("Sample job executed successfully");
            return Task.CompletedTask;
        }
    }
} 