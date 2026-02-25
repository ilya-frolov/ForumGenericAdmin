using System.ComponentModel.DataAnnotations.Schema;
using Dino.Core.AdminBL.Models;

namespace DinoGenericAdmin.BL.Models
{
    public class AdminUser : AdminUserBase<AdminUser, AdminRole>
    {
        // Inherits all properties from AdminUserBase
    }
} 