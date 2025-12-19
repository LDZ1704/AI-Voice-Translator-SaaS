using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AI_Voice_Translator_SaaS.Models;

[Index("Email", Name = "UQ__Users__A9D10534F3DCF3A1", IsUnique = true)]
public partial class User
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(256)]
    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    [StringLength(100)]
    public string DisplayName { get; set; } = null!;

    [StringLength(20)]
    public string Role { get; set; } = null!;

    [StringLength(20)]
    public string SubscriptionTier { get; set; } = null!;

    public DateTime? SubscriptionExpiryDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<AudioFile> AudioFiles { get; set; } = new List<AudioFile>();

    [InverseProperty("User")]
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
