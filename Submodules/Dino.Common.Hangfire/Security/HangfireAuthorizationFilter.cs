using Dino.Common.Hangfire.Configuration;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Dino.Common.Hangfire.Security
{
    /// <summary>
    /// Authorization filter for the Hangfire dashboard based on IP address
    /// </summary>
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly ILogger<HangfireAuthorizationFilter> _logger;
        private readonly IOptions<HangfireConfig> _config;

        public HangfireAuthorizationFilter(
            IOptions<HangfireConfig> config, 
            ILogger<HangfireAuthorizationFilter> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Authorizes access to the Hangfire dashboard
        /// </summary>
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            var allowedIps = _config.Value.DashboardAllowedIps;

            // If no allowed IPs are configured, only allow localhost
            bool isLocalhost = false;
            if (allowedIps == null || !allowedIps.Any())
            {
                isLocalhost = (httpContext.Connection.RemoteIpAddress?.Equals(httpContext.Connection.LocalIpAddress) ?? false);
                return isLocalhost;
            }

            var remoteIpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var azureClientIp = httpContext.Request.Headers["X-Azure-ClientIP"].ToString();
            var azureSocketIp = httpContext.Request.Headers["X-Azure-SocketIP"].ToString();
            var forwardedForIp = httpContext.Request.Headers["X-Forwarded-For"].ToString();

            isLocalhost = (httpContext.Connection.RemoteIpAddress?.Equals(httpContext.Connection.LocalIpAddress) ?? false);

            // Allow only users from localhost or configured IP addresses
            var isAllowed = isLocalhost ||
                           (remoteIpAddress != null && allowedIps.Contains(remoteIpAddress)) ||
                           (!string.IsNullOrEmpty(azureClientIp) && allowedIps.Contains(azureClientIp)) ||
                           (!string.IsNullOrEmpty(azureSocketIp) && allowedIps.Contains(azureSocketIp)) ||
                           (!string.IsNullOrEmpty(forwardedForIp) && allowedIps.Contains(forwardedForIp));

            if (!isAllowed)
            {
                _logger.LogInformation(
                    "Hangfire Denied Access. Remote IP: {RemoteIp}, Azure Client IP: {AzureClientIp}, " +
                    "Azure Socket IP: {AzureSocketIp}, Forwarded For IP: {ForwardedForIp}",
                    remoteIpAddress, azureClientIp, azureSocketIp, forwardedForIp);
            }

            return isAllowed;
        }
    }
} 