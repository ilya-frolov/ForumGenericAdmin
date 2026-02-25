using Dino.CoreMvc.Admin.Models;
using Dino.CoreMvc.Admin.Models.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dino.CoreMvc.Admin.Logic
{
    public static class AdminServiceExtensions
    {
        public static IServiceCollection AddAdminServices(this IServiceCollection services, IConfiguration configuration)
        {
            var apiConfigSection = configuration.GetSection("AdminConfig");
            services.Configure<AdminConfig>(apiConfigSection);

            return services;
        }
    }
}
