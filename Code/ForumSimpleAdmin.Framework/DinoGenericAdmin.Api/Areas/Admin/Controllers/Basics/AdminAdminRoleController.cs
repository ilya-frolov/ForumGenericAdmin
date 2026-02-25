using DinoGenericAdmin.Api.Areas.Admin.Models.Basics;
using Dino.CoreMvc.Admin.Controllers.Admin;
using DinoGenericAdmin.BL.Models;
using Microsoft.AspNetCore.Mvc;

namespace DinoGenericAdmin.Api.Areas.Admin.Controllers.Basics
{
    public class AdminAdminRoleController : AdminAdminRoleControllerBase<AdminAdminRoleModel, AdminRole, AdminUser>
    {
        // Inherits all functionality from AdminAdminRoleControllerBase
    }
} 