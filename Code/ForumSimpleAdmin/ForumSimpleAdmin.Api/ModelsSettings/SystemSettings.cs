using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.FieldTypePlugins;
using Dino.CoreMvc.Admin.ModelsSettings;

namespace ForumSimpleAdmin.Api.ModelsSettings
{
    public class SystemSettings : SystemSettingsBase
    {
        [AdminFieldCommon("Site Locked", tooltip: "When locked, all public forum operations are blocked.")]
        [AdminFieldCheckbox]
        [ListSettings]
        public bool IsSiteLocked { get; set; }
    }
}
