using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dino.Core.AdminBL.Models
{
    public class AdminUserBase<TUser, TRole>
        where TRole : AdminRoleBase<TRole, TUser>, new()
        where TUser : AdminUserBase<TUser, TRole>, new()
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(256)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(50)]
        public string Phone { get; set; }

        [Required]
        public int RoleId { get; set; }

        [Required]
        public byte[] PasswordHash { get; set; }

        [Required]
        public byte[] PasswordSalt { get; set; }

        [StringLength(100)]
        public string EmailVerificationCode { get; set; }

        public DateTime? VerificationCodeDate { get; set; }

        [StringLength(255)]
        public string PictureUrl { get; set; }

        [Required]
        public bool Active { get; set; } = true;

        [Required]
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;

        [Required]
        public int UpdateBy { get; set; } = 1;

        public DateTime? LastLoginDate { get; set; }

        [StringLength(50)]
        public string LastIpAddress { get; set; }

        [Required]
        public bool Archived { get; set; } = false;

        // Navigation property
        [ForeignKey("RoleId")]
        public virtual TRole Role { get; set; }
    }
} 