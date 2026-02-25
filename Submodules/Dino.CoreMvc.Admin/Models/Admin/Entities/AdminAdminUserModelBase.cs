using System.ComponentModel.DataAnnotations;
using Dino.Common.Helpers;
using Dino.Common.Security;
using Dino.Core.AdminBL.Models;
using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.Logic;
using Dino.CoreMvc.Admin.Models;

namespace Dino.CoreMvc.Admin.Models.Admin.Entities
{
    public class AdminAdminUserModelBase : BaseAdminModel
    {
        [Tab("General")]
        [Container("Basic Information", "User details")]
        [AdminFieldCommon("ID", readOnly: true)]
        [AdminFieldNumber]
        [VisibilitySettings(showOnCreate: false)]
        [ListSettings]
        public int Id { get; set; }

        [AdminFieldCommon("Email", required: true, tooltip: "User email")]
        [AdminFieldText(maxLength: 256)]
        [EmailAddress]
        [ListSettings]
        public string Email { get; set; }

        [AdminFieldCommon("Full Name", required: true, tooltip: "User full name")]
        [AdminFieldText(maxLength: 100)]
        [ListSettings]
        public string FullName { get; set; }

        [AdminFieldCommon("Phone", tooltip: "User phone number")]
        [AdminFieldText(maxLength: 50)]
        public string Phone { get; set; }

        [AdminFieldCommon("Role", required: true, tooltip: "User role")]
        [AdminFieldSelect(SelectSourceType.Function, "GetAvailableRoles")]
        [ListSettings]
        public int RoleId { get; set; }

        [EndContainer]
        [Container("Security", "Security settings")]
        [AdminFieldCommon("Password", required: false, tooltip: "User password")]
        [AdminFieldPassword]
        [SkipMapping(true, true)]
        public string Password { get; set; }

        [AdminFieldCommon("Confirm Password", required: true, tooltip: "Confirm password")]
        [AdminFieldPassword]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [HideIf(nameof(Password), null)]
        [SkipMapping(true, true)]
        public string ConfirmPassword { get; set; }

        [EndContainer]
        [Container("Profile", "Profile information")]
        [AdminFieldCommon("Profile Picture", tooltip: "User profile picture")]
        [AdminFieldPicture(new[] { "jpg", "png", "webp" }, ForcePictureFormat.WebP, maxSize: 2)]
        public string PictureUrl { get; set; }

        [EndContainer]
        [EndTab]
        [Tab("System")]
        [Container("Status", "User status")]
        [AdminFieldCommon("Active")]
        [AdminFieldCheckbox(allowListToggle: true)]
        [ListSettings]
        public bool Active { get; set; } = true;

        [AdminFieldCommon("Archived", readOnly: true)]
        [AdminFieldCheckbox]
        [VisibilitySettings(showOnCreate: false)]
        [ArchiveIndicator]
        public bool Archived { get; set; }

        [EndContainer]
        [Container("Audit", "Audit information")]
        [AdminFieldCommon("Created Date", readOnly: true)]
        [AdminFieldDateTime]
        [VisibilitySettings(showOnCreate: false)]
        [SaveDate]
        public DateTime CreateDate { get; set; }

        [AdminFieldCommon("Last Updated", readOnly: true)]
        [AdminFieldDateTime]
        [VisibilitySettings(showOnCreate: false)]
        [LastUpdateDate]
        public DateTime UpdateDate { get; set; }

        [AdminFieldCommon("Last Login", readOnly: true)]
        [AdminFieldDateTime]
        [VisibilitySettings(showOnCreate: false)]
        public DateTime? LastLoginDate { get; set; }

        [EndContainer]
        [EndTab]
        [AdminFieldCommon("Last IP", readOnly: true)]
        [AdminFieldText]
        [VisibilitySettings(showOnCreate: false)]
        public string LastIpAddress { get; set; }
    }
} 