using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dino.CoreMvc.Admin.Models.Admin
{
    public class AdminConfig
    {
        public bool AllowHebrew { get; set; }
        public bool AllowEnglish { get; set; }
        public string NewServerUrl { get; set; }

        public LoginSecurityConfig LoginSecurityConfig { get; set; }
    }

    public class LoginSecurityConfig
    {
        public int OtpExpiryMinutes { get; set; }
        public int OtpLength { get; set; }
    }
}
