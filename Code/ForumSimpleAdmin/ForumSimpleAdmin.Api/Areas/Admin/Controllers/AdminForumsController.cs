using Dino.CoreMvc.Admin.Controllers;
using Dino.CoreMvc.Admin.Models;
using Dino.CoreMvc.Admin.Models.Admin;
using ForumSimpleAdmin.Api.Areas.Admin.Models;
using ForumSimpleAdmin.BL.Models;

namespace ForumSimpleAdmin.Api.Areas.Admin.Controllers
{
    public class AdminForumsController : DinoAdminBaseEntityController<AdminForumModel, Forum, int>
    {
        public AdminForumsController()
            : base("forums")
        {
        }

        protected override async Task<AdminSegment> CreateAdminSegment()
        {
            AdminSegment segment = new AdminSegment
            {
                General = new AdminSegmentGeneral
                {
                    Name = "Forums",
                    Priority = 10,
                    MenuHeader = "Forum"
                },
                UI = new AdminSegmentUI
                {
                    Icon = "comments",
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
                Title = "Forums",
                AllowReOrdering = true,
                AllowAdd = true,
                AllowEdit = true,
                AllowDelete = false,
                ShowDeleteConfirmation = false
            };

            return await Task.FromResult(listDef);
        }
    }
}
