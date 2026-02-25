using Dino.Core.AdminBL.Contracts;

namespace ForumSimpleAdmin.BL.Contracts
{
    public class BlConfig : BaseBlConfig
    {
        public EmailsConfig EmailsConfig { get; set; } = new EmailsConfig();
    }

    public class EmailsConfig
    {
        public string EmailSmtpHost { get; set; } = string.Empty;
        public int EmailSmtpPort { get; set; }
        public bool EmailSmtpUseSsl { get; set; }
        public string EmailSmtpUser { get; set; } = string.Empty;
        public string EmailSmtpPassword { get; set; } = string.Empty;
        public string EmailFromAddress { get; set; } = string.Empty;
        public string EmailFromName { get; set; } = string.Empty;
    }
}
