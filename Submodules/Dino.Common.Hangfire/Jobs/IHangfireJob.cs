namespace Dino.Common.Hangfire.Jobs
{
    /// <summary>
    /// Base interface for all Hangfire jobs
    /// </summary>
    public interface IHangfireJob
    {
        /// <summary>
        /// Gets the job name, which should be unique
        /// </summary>
        string JobName { get; }

        /// <summary>
        /// Gets the CRON schedule expression for recurring jobs
        /// </summary>
        string CronSchedule { get; }
        
        /// <summary>
        /// Gets the queue name for the job
        /// </summary>
        string Queue { get; }
    }
} 