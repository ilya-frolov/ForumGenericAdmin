using System.ComponentModel.DataAnnotations;

namespace Dino.Core.AdminBL.Models
{
    public class Setting
    {
        /// <summary>
        /// The name of the class (from the assembly).
        /// </summary>
        [Key]
        [Required]
        [StringLength(100)]
        public string ClassName { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Data { get; set; }

        [Required]
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;

        [Required]
        public int UpdateBy { get; set; } = 1;
    }
} 