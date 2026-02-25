using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.FieldTypePlugins;
using Dino.CoreMvc.Admin.Logic;
using Dino.CoreMvc.Admin.Models.Admin;
using System.Security.Cryptography;
using System.Text;

namespace ForumSimpleAdmin.Api.Areas.Admin.Models
{
    public class AdminForumModel : BaseAdminModel
    {
        [AdminFieldCommon("ID", readOnly: true)]
        [AdminFieldNumber]
        [VisibilitySettings(showOnCreate: false)]
        [ListSettings]
        public int Id { get; set; }

        [AdminFieldCommon("Order", readOnly: true)]
        [AdminFieldNumber]
        [SortIndex]
        [ListSettings]
        public int SortIndex { get; set; }

        [AdminFieldCommon("Forum Name", required: true)]
        [AdminFieldText(maxLength: 100)]
        [ListSettings]
        public string Name { get; set; } = string.Empty;

        [AdminFieldCommon("Managers Only Posting")]
        [AdminFieldCheckbox]
        [ListSettings]
        public bool ManagersOnlyPosting { get; set; }

        [AdminFieldCommon("Active")]
        [AdminFieldCheckbox(allowListToggle: true)]
        [ListSettings]
        public bool Active { get; set; } = true;

        [AdminFieldCommon("Deleted", readOnly: true)]
        [AdminFieldCheckbox]
        [VisibilitySettings(false)]
        [DeletionIndicator]
        public bool IsDeleted { get; set; }

        [AdminFieldCommon("Created Date", readOnly: true)]
        [AdminFieldDateTime]
        [VisibilitySettings(false)]
        [SaveDate]
        public DateTime CreateDate { get; set; }

        [AdminFieldCommon("Last Updated", readOnly: true)]
        [AdminFieldDateTime]
        [VisibilitySettings(false)]
        [LastUpdateDate]
        public DateTime UpdateDate { get; set; }

        [AdminFieldCommon("Updated By", readOnly: true)]
        [AdminFieldNumber]
        [VisibilitySettings(false)]
        [UpdatedBy]
        public int UpdateBy { get; set; }
    }

    public class AdminForumUserModel : BaseAdminModel
    {
        [AdminFieldCommon("ID", readOnly: true)]
        [AdminFieldNumber]
        [VisibilitySettings(showOnCreate: false)]
        [ListSettings]
        public int Id { get; set; }

        [AdminFieldCommon("User Name", required: true)]
        [AdminFieldText(maxLength: 100)]
        [ListSettings]
        public string Name { get; set; } = string.Empty;

        [AdminFieldCommon("Password", required: true, tooltip: "Set only on create or change.")]
        [AdminFieldPassword]
        [VisibilitySettings(showOnView: false)]
        public string PasswordHash { get; set; } = string.Empty;

        [AdminFieldCommon("Profile Picture")]
        [AdminFieldPicture(new[] { "jpg", "jpeg", "png", "webp" }, ForcePictureFormat.NO_FORCE, maxSize: 4)]
        public FileContainerCollection? ProfilePicturePath { get; set; }

        [AdminFieldCommon("Manager")]
        [AdminFieldCheckbox]
        [ListSettings]
        public bool IsManager { get; set; }

        [AdminFieldCommon("Deleted", readOnly: true)]
        [AdminFieldCheckbox]
        [VisibilitySettings(false)]
        [DeletionIndicator]
        public bool IsDeleted { get; set; }

        [AdminFieldCommon("Created Date", readOnly: true)]
        [AdminFieldDateTime]
        [VisibilitySettings(false)]
        [SaveDate]
        public DateTime CreateDate { get; set; }

        [AdminFieldCommon("Last Updated", readOnly: true)]
        [AdminFieldDateTime]
        [VisibilitySettings(false)]
        [LastUpdateDate]
        public DateTime UpdateDate { get; set; }

        [AdminFieldCommon("Updated By", readOnly: true)]
        [AdminFieldNumber]
        [VisibilitySettings(false)]
        [UpdatedBy]
        public int UpdateBy { get; set; }

        public override bool CustomPreMapToDbModel(dynamic model, dynamic dbModel, ModelMappingContext context)
        {
            string incomingPassword = model.PasswordHash ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(incomingPassword))
            {
                dbModel.PasswordHash = HashPassword(incomingPassword);
            }

            return true;
        }

        public override void CustomPostMapFromDbModel(dynamic dbModel, dynamic model, ModelMappingContext context)
        {
            model.PasswordHash = string.Empty;
        }

        private static string HashPassword(string password)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = SHA256.HashData(bytes);
            string hashString = Convert.ToHexString(hash);
            return hashString;
        }
    }

    public class AdminSiteSettingsModel : BaseAdminModel
    {
        [AdminFieldCommon("ID", readOnly: true)]
        [AdminFieldNumber]
        [VisibilitySettings(showOnCreate: false)]
        [ListSettings]
        public int Id { get; set; }

        [AdminFieldCommon("Site Locked", tooltip: "When true, login and posting APIs are blocked.")]
        [AdminFieldCheckbox]
        [ListSettings]
        public bool IsLocked { get; set; }

        [AdminFieldCommon("Created Date", readOnly: true)]
        [AdminFieldDateTime]
        [VisibilitySettings(false)]
        [SaveDate]
        public DateTime CreateDate { get; set; }

        [AdminFieldCommon("Last Updated", readOnly: true)]
        [AdminFieldDateTime]
        [VisibilitySettings(false)]
        [LastUpdateDate]
        public DateTime UpdateDate { get; set; }

        [AdminFieldCommon("Updated By", readOnly: true)]
        [AdminFieldNumber]
        [VisibilitySettings(false)]
        [UpdatedBy]
        public int UpdateBy { get; set; }
    }
}
