using System.ComponentModel.DataAnnotations;

namespace Dino.Core.AdminBL.Models
{
    public class AdminRoleBase<TRole, TUser>
        where TRole : AdminRoleBase<TRole, TUser>, new()
        where TUser : AdminUserBase<TUser, TRole>, new()
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        public short RoleType { get; set; } // 0 = DinoAdmin, 1 = RegularAdmin, 2 = Custom

        [Required]
        public bool IsVisible { get; set; } = true;

        [Required]
        public bool IsSystemDefined { get; set; } = false;

        // Navigation property
        public virtual ICollection<TUser> Users { get; set; }
    }
} 