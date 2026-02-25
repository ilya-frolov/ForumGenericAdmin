using Dino.CoreMvc.Admin.Controllers;
using Dino.CoreMvc.Admin.Models;
using Dino.CoreMvc.Admin.Models.Admin;
using ForumSimpleAdmin.Api.Areas.Admin.Models;
using ForumSimpleAdmin.BL.Models;

namespace ForumSimpleAdmin.Api.Areas.Admin.Controllers
{
    public class AdminSiteSettingsController : DinoAdminBaseEntityController<AdminSiteSettingsModel, SiteSettings, int>
    {
        public AdminSiteSettingsController()
            : base("site-settings")
        {
        }

        protected override async Task<AdminSegment> CreateAdminSegment()
        {
            AdminSegment segment = new AdminSegment
            {
                General = new AdminSegmentGeneral
                {
                    Name = "Site Settings",
                    Priority = 100,
                    MenuHeader = "System"
                },
                UI = new AdminSegmentUI
                {
                    Icon = "cog",
                    IconType = IconType.PrimeIcons,
                    ShowInMenu = true
                }
            };

            return await Task.FromResult(segment);
        }

        protected override async Task<ListDef> CreateListDef(string refId = null)
        {
            ListDef listDef = new ListDef
            {
                Title = "Site Settings",
                AllowReOrdering = false,
                AllowAdd = false,
                AllowEdit = true,
                AllowDelete = false
            };

            return await Task.FromResult(listDef);
        }
    }
}
