using Dino.CoreMvc.Admin.Contracts;

namespace ForumSimpleAdmin.Api.Models
{
    public class ApiConfig : BaseApiConfig
    {
        public ServiceSettings ServiceSettings { get; set; } = new ServiceSettings();
    }

    public class ServiceSettings
    {
        public bool DisableServices { get; set; }
    }
}
