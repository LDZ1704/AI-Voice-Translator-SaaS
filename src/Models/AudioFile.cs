using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AI_Voice_Translator_SaaS.Models;

[Index("Status", Name = "IX_AudioFiles_Status")]
[Index("UserId", Name = "IX_AudioFiles_UserId")]
public partial class AudioFile
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [StringLength(255)]
    public string FileName { get; set; } = null!;

    [StringLength(500)]
    public string OriginalFileUrl { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    public int? DurationSeconds { get; set; }

    public DateTime UploadedAt { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [InverseProperty("AudioFile")]
    public virtual Transcript? Transcripts { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("AudioFiles")]
    public virtual User User { get; set; } = null!;
}
