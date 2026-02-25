using Dino.Core.AdminBL.Models;
using Dino.CoreMvc.Admin.Controllers;
using Dino.CoreMvc.Admin.Controllers.Admin;
using DinoGenericAdmin.Api.Areas.Admin.Models.Basics;
using DinoGenericAdmin.BL.Models;

namespace DinoGenericAdmin.Api.Areas.Admin.Controllers.Basics
{
    public class AdminAdminUserController : AdminAdminUserControllerBase<AdminAdminUserModel, AdminUser, AdminRole>
    {
    }
} 