using Dino.Common.Hangfire.Jobs;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Dino.Common.Hangfire
{
    /// <summary>
    /// Extension methods for registering Hangfire jobs
    /// </summary>
    public static class JobRegistrationExtensions
    {
        /// <summary>
        /// Registers a job type in the DI container
        /// </summary>
        /// <typeparam name="TJob">The job type that implements IHangfireJob</typeparam>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddHangfireJob<TJob>(
            this IServiceCollection services) 
            where TJob : class, IHangfireJob
        {
            services.AddTransient<IHangfireJob, TJob>();
            services.AddTransient<TJob>();
            return services;
        }

        /// <summary>
        /// Registers a job with a factory function in the DI container
        /// </summary>
        /// <typeparam name="TJob">The job type that implements IHangfireJob</typeparam>
        /// <param name="services">The service collection</param>
        /// <param name="factory">Factory function to create the job</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddHangfireJob<TJob>(
            this IServiceCollection services,
            Func<IServiceProvider, TJob> factory) 
            where TJob : class, IHangfireJob
        {
            services.AddTransient<IHangfireJob>(sp => factory(sp));
            services.AddTransient(factory);
            return services;
        }

        /// <summary>
        /// Registers a job instance in the DI container
        /// </summary>
        /// <typeparam name="TJob">The job type that implements IHangfireJob</typeparam>
        /// <param name="services">The service collection</param>
        /// <param name="jobInstance">The job instance</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddHangfireJob<TJob>(
            this IServiceCollection services,
            TJob jobInstance) 
            where TJob : class, IHangfireJob
        {
            services.AddSingleton<IHangfireJob>(jobInstance);
            services.AddSingleton(jobInstance);
            return services;
        }
    }
} 