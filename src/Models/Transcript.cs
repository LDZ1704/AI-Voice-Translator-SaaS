using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AI_Voice_Translator_SaaS.Models;

[Index("AudioFileId", Name = "IX_Transcripts_AudioFileId")]
public partial class Transcript
{
    [Key]
    public Guid Id { get; set; }

    public Guid AudioFileId { get; set; }

    public string OriginalText { get; set; } = null!;

    [StringLength(10)]
    public string DetectedLanguage { get; set; } = null!;

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? Confidence { get; set; }

    public DateTime ProcessedAt { get; set; }

    [ForeignKey("AudioFileId")]
    [InverseProperty("Transcripts")]
    public virtual AudioFile AudioFile { get; set; } = null!;

    [InverseProperty("Transcript")]
    public virtual ICollection<Translation> Translations { get; set; } = new List<Translation>();
}
