using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.Models.Admin;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Dino.CoreMvc.Admin.Attributes.Permissions;
using Dino.CoreMvc.Admin.Models.Admin.Entities;

namespace Dino.CoreMvc.Admin.ModelsSettings
{
    [Description("Dino Master Settings")]
    [AdminPermission((short)RoleType.DinoAdmin)]
    public abstract class DinoMasterSettingsBase : AdminBaseSettings
    {
        [AdminFieldCommon("Site Name", required: true)]
        [AdminFieldText(maxLength: 100)]
        public string SiteName { get; set; }

        [AdminFieldCommon("Site Logo")]
        [AdminFieldPicture(null, platforms: Platforms.Desktop)]
        public string SiteLogo { get; set; }        // TODO: Replace with the relevant file model.

        [AdminFieldCommon("Site Description")]
        [AdminFieldText]
        public string SiteDescription { get; set; }

        [AdminFieldCommon("Customer Name")]
        [AdminFieldText]
        public string CustomerName { get; set; }

        [AdminFieldCommon("Customer Logo")]
        [AdminFieldPicture(null, platforms: Platforms.Desktop)]
        public string CustomerLogo { get; set; }        // TODO: Replace with the relevant file model.

        [AdminFieldCommon("Contact Email")]
        [AdminFieldText(maxLength: 256)]
        [EmailAddress]
        public string ContactEmail { get; set; }

        [AdminFieldCommon("Maintenance Mode")]
        [AdminFieldCheckbox]
        public bool MaintenanceMode { get; set; }

        [AdminFieldCommon("Require Otp On Login")]
        [AdminFieldCheckbox()]
        public bool? RequireOtpOnLogin { get; set; }

        [AdminFieldCommon("Email Settings")]
        [ComplexType(typeof(EmailSettingsConfig), true)]
        public EmailSettingsConfig EmailSettings { get; set; }
    }

    public class EmailSettingsConfig : BaseAdminModel
    {
        [Container("SMTP Server")]
        [AdminFieldCommon("SMTP Host")]
        [AdminFieldText("Ex: smtp.domain.com")]
        public string SmtpHost { get; set; }

        [AdminFieldCommon("SMTP Port", "25 / 465 / 587")]
        [AdminFieldNumber()]
        public int SmtpPort { get; set; }

        [AdminFieldCommon("Enable SSL")]
        [AdminFieldCheckbox()]
        public bool EnableSsl { get; set; }

        [AdminFieldCommon("SMTP Username", "If authentication required")]
        [AdminFieldText()]
        public string SmtpUser { get; set; }

        [EndContainer]
        [AdminFieldCommon("SMTP Password", "If authentication required")]
        [AdminFieldText()]
        public string SmtpPassword { get; set; }

        [Container("Sender Details")]
        [AdminFieldCommon("Sender Email Address", "The email address that will be shown as the origin")]
        [AdminFieldText()]
        public string FromEmail { get; set; }

        [EndContainer]
        [AdminFieldCommon("Sender Name", "The name that will be shown next to the email address")]
        [AdminFieldText()]
        public string FromName { get; set; }
    }
}
