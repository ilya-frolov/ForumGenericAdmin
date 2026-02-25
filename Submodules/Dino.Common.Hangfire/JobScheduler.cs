using Dino.Common.Hangfire.Jobs;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Common;

namespace Dino.Common.Hangfire
{
    /// <summary>
    /// Handles registering and scheduling Hangfire jobs
    /// </summary>
    public class JobScheduler
    {
        private readonly ILogger<JobScheduler> _logger;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly IServiceProvider _serviceProvider;

        public JobScheduler(
            ILogger<JobScheduler> logger,
            IRecurringJobManager recurringJobManager,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _recurringJobManager = recurringJobManager;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Register all jobs with Hangfire
        /// </summary>
        public void RegisterAllJobs()
        {
            // Create a scope for resolving scoped services
            using var scope = _serviceProvider.CreateScope();
            var jobs = scope.ServiceProvider.GetServices<IHangfireJob>().ToList();
            
            if (jobs == null || !jobs.Any())
            {
                _logger.LogInformation("No Hangfire jobs found to register");
                return;
            }

            foreach (var job in jobs)
            {
                _logger.LogInformation("Registering job {JobName} with schedule {CronSchedule} on queue {Queue}", 
                    job.JobName, job.CronSchedule, job.Queue);

                // Register job using IServiceProvider to resolve the job instance at runtime
                RegisterJob(job.GetType(), job.JobName, job.CronSchedule, job.Queue);
            }

            _logger.LogInformation("Registered {JobCount} Hangfire jobs", jobs.Count());
        }

        /// <summary>
        /// Registers a job with Hangfire
        /// </summary>
        private void RegisterJob(Type jobType, string jobName, string cronSchedule, string queue)
        {
            // Use job factory pattern to create the job from the service provider
            _recurringJobManager.AddOrUpdate(
                jobName,
                Job.FromExpression(() => ExecuteJob(jobType)),
                cronSchedule,
                new RecurringJobOptions
                {
                    QueueName = queue
                });
        }

        /// <summary>
        /// Executes a job by resolving it from the service provider
        /// </summary>
        public void ExecuteJob(Type jobType)
        {
            try
            {
                // Create a scope and resolve the job
                using var scope = _serviceProvider.CreateScope();
                var job = scope.ServiceProvider.GetService(jobType) as BaseHangfireJob;
                
                if (job == null)
                {
                    _logger.LogError("Failed to resolve job of type {JobType}", jobType.Name);
                    return;
                }

                // Execute the job
                job.ExecuteAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing job of type {JobType}: {Message}", jobType.Name, ex.Message);
                throw; // Re-throw so Hangfire records the failure
            }
        }
    }
} 