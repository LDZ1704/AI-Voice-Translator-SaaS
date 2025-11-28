using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AI_Voice_Translator_SaaS.Models;

[Index("TranscriptId", Name = "IX_Translations_TranscriptId")]
public partial class Translation
{
    [Key]
    public Guid Id { get; set; }

    public Guid TranscriptId { get; set; }

    [StringLength(10)]
    public string TargetLanguage { get; set; } = null!;

    public string TranslatedText { get; set; } = null!;

    [StringLength(50)]
    public string TranslationEngine { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public int? UserRating { get; set; }

    [InverseProperty("Translation")]
    public virtual Output? Outputs { get; set; }

    [ForeignKey("TranscriptId")]
    [InverseProperty("Translations")]
    public virtual Transcript Transcript { get; set; } = null!;
}
