using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dino.Core.AdminBL.Contracts;

namespace DinoGenericAdmin.BL.Contracts
{
    public class BlConfig : BaseBlConfig
    {
        public EmailsConfig EmailsConfig { get; set; }
    }

    public class EmailsConfig
    {
        public string EmailSmtpHost { get; set; }
        public int EmailSmtpPort { get; set; }
        public bool EmailSmtpUseSsl { get; set; }
        public string EmailSmtpUser { get; set; }
        public string EmailSmtpPassword { get; set; }
        public string EmailFromAddress { get; set; }
        public string EmailFromName { get; set; }
    }
}
