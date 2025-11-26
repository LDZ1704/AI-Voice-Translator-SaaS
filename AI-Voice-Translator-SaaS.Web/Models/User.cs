using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AI_Voice_Translator_SaaS.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }

        public string? PasswordHash { get; set; }

        [Required]
        [MaxLength(100)]
        public string DisplayName { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "User"; // User, Admin

        [Required]
        [MaxLength(20)]
        public string SubscriptionTier { get; set; } = "Free"; // Free, Premium

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<AudioFile> AudioFiles { get; set; } = new List<AudioFile>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
