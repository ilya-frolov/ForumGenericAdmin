using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dino.CoreMvc.Admin.Contracts
{
    public class BaseApiConfig
    {
        public string ApiBaseUrl { get; set; }
        public string UploadsFolder { get; set; }
        public string AllowCorsOrigins { get; set; }
        public string DateTimeStringFormat { get; set; }
        public string BaseCdnUrl { get; set; }
    }
}
