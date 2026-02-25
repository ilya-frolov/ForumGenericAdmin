using Dino.Core.AdminBL.Models;
using Dino.CoreMvc.Admin.Attributes.Permissions;
using Dino.CoreMvc.Admin.Controllers;
using Dino.CoreMvc.Admin.Models;
using Dino.CoreMvc.Admin.Models.Admin;
using Dino.CoreMvc.Admin.Models.Admin.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Dino.CoreMvc.Admin.Controllers.Admin
{
    [AdminPermission((short)RoleType.DinoAdmin)]
    public class AdminAdminRoleControllerBase<TAdminRoleModel, TAdminRole, TAdminUser> : DinoAdminBaseEntityController<TAdminRoleModel, TAdminRole, int>
        where TAdminRoleModel : AdminAdminRoleModelBase, new()
        where TAdminUser : AdminUserBase<TAdminUser, TAdminRole>, new()
        where TAdminRole : AdminRoleBase<TAdminRole, TAdminUser>, new()
    {
        public AdminAdminRoleControllerBase() : base("admin_roles")
        {
        }

        protected override async Task<AdminSegment> CreateAdminSegment()
        {
            return new AdminSegment
            {
                General = new AdminSegmentGeneral
                {
                    Name = "הרשאות ניהול",
                    Priority = 1
                },
                UI = new AdminSegmentUI
                {
                    Icon = "users",
                    IconType = IconType.PrimeIcons,
                    ShowInMenu = true,
                },
                Navigation = new AdminSegmentNavigation
                {
                    CustomPath = null,
                },
            };
        }

        protected override async Task<ListDef> CreateListDef(string refId = null)
        {
            return new ListDef
            {
                Title = "Roles List",
                AllowReOrdering = false,
                AllowAdd = true,
                AllowEdit = true,
                AllowDelete = true,
                ShowArchive = false,
                ShowDeleteConfirmation = true,
            };
        }

        //protected override void OnBeforeSave(TAdminRoleModel model, TAdminRole entity, bool isNew)
        //{
        //    base.OnBeforeSave(model, entity, isNew);

        //    // Prevent editing system-defined roles
        //    if (!isNew && entity.IsSystemDefined)
        //    {
        //        throw new InvalidOperationException("Cannot modify system-defined roles");
        //    }
        //}

        //protected override void OnBeforeDelete(TAdminRole entity)
        //{
        //    base.OnBeforeDelete(entity);

        //    // Prevent deleting system-defined roles
        //    if (entity.IsSystemDefined)
        //    {
        //        throw new InvalidOperationException("Cannot delete system-defined roles");
        //    }

        //    // Check if role has any users
        //    if (entity.Users?.Any() == true)
        //    {
        //        throw new InvalidOperationException("Cannot delete role that has assigned users");
        //    }
        //}
    }
} 