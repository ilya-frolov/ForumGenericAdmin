using Dino.CoreMvc.Admin.Controllers;
using Dino.CoreMvc.Admin.Models;
using Dino.CoreMvc.Admin.Models.Admin;
using ForumSimpleAdmin.Api.Areas.Admin.Models;
using ForumSimpleAdmin.BL.Models;

namespace ForumSimpleAdmin.Api.Areas.Admin.Controllers
{
    public class AdminForumUsersController : DinoAdminBaseEntityController<AdminForumUserModel, ForumUser, int>
    {
        public AdminForumUsersController()
            : base("forum-users")
        {
        }

        protected override async Task<AdminSegment> CreateAdminSegment()
        {
            AdminSegment segment = new AdminSegment
            {
                General = new AdminSegmentGeneral
                {
                    Name = "Forum Users",
                    Priority = 20
                },
                UI = new AdminSegmentUI
                {
                    Icon = "users",
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
                Title = "Forum Users",
                AllowAdd = true,
                AllowEdit = true,
                AllowDelete = false,
                ShowDeleteConfirmation = false
            };

            return await Task.FromResult(listDef);
        }
    }
}
