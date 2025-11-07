using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pos_app.Models
{
    public class SuperAdmin
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLoginAt { get; set; }
        
        public bool IsActive { get; set; } = true;

        // Forgot Password Fields (not stored in database for now)
        [NotMapped]
        public string? VerificationCode { get; set; }
        [NotMapped]
        public DateTime? VerificationCodeExpiry { get; set; }
        [NotMapped]
        public string? ResetToken { get; set; }
        [NotMapped]
        public DateTime? ResetTokenExpiry { get; set; }

        // Password field for compatibility (maps to PasswordHash)
        [NotMapped]
        public string Password 
        { 
            get => PasswordHash; 
            set => PasswordHash = value; 
        }
    }
}
