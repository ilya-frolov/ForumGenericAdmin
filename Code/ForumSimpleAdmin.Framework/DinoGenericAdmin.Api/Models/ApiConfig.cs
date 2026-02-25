using Dino.CoreMvc.Admin.Contracts;

namespace DinoGenericAdmin.Api.Models
{
    public class ApiConfig : BaseApiConfig
    {
        public ServiceSettings ServiceSettings { get; set; }
    }

    public class ServiceSettings
    {
        public bool DisableServices { get; set; }
    }
}
