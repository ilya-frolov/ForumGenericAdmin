using System.ComponentModel;
using Dino.Core.AdminBL.Models;
using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.Models;

namespace Dino.CoreMvc.Admin.Models.Admin.Entities
{
    public class AdminAdminRoleModelBase : BaseAdminModel
    {
        [Tab("General")]
        [Container("Basic Information", "Role details")]
        [AdminFieldCommon("ID", readOnly: true)]
        [AdminFieldNumber]
        [VisibilitySettings(showOnCreate: false)]
        [ListSettings]
        public int Id { get; set; }

        [AdminFieldCommon("Name", required: true, tooltip: "Role name")]
        [AdminFieldText(maxLength: 100)]
        [ListSettings]
        public string Name { get; set; }

        [AdminFieldCommon("Description", tooltip: "Role description")]
        [AdminFieldTextArea]
        public string Description { get; set; }

        [AdminFieldCommon("Role DateType", required: true, tooltip: "DateType of role")]
        [AdminFieldSelect(SelectSourceType.Enum, typeof(RoleType))]
        [ListSettings]
        public short RoleType { get; set; }

        [AdminFieldCommon("Visible")]
        [AdminFieldCheckbox(allowListToggle: true)]
        [ListSettings]
        public bool IsVisible { get; set; } = true;

        [EndContainer]
        [EndTab]
        [AdminFieldCommon("System Defined", readOnly: true)]
        [AdminFieldCheckbox]
        [VisibilitySettings(showOnCreate: false)]
        public bool IsSystemDefined { get; set; } = false;
    }

    public enum RoleType : short
    {
        [Description("Dino Admin")]
        DinoAdmin = 0,
        [Description("Regular Admin")]
        RegularAdmin = 1,
        [Description("Custom")]
        Custom = 2
    }
} 