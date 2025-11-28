using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AI_Voice_Translator_SaaS.Models;

[Index("Timestamp", Name = "IX_AuditLogs_Timestamp")]
[Index("UserId", Name = "IX_AuditLogs_UserId")]
public partial class AuditLog
{
    [Key]
    public int Id { get; set; }

    public Guid? UserId { get; set; }

    [StringLength(100)]
    public string Action { get; set; } = null!;

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public DateTime Timestamp { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("AuditLogs")]
    public virtual User? User { get; set; }
}
