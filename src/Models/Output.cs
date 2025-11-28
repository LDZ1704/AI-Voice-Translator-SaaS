using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AI_Voice_Translator_SaaS.Models;

[Index("TranslationId", Name = "IX_Outputs_TranslationId")]
public partial class Output
{
    [Key]
    public Guid Id { get; set; }

    public Guid TranslationId { get; set; }

    [StringLength(500)]
    public string OutputFileUrl { get; set; } = null!;

    [StringLength(20)]
    public string VoiceType { get; set; } = null!;

    [StringLength(50)]
    public string VoiceModel { get; set; } = null!;

    public DateTime GeneratedAt { get; set; }

    public int DownloadCount { get; set; }

    public DateTime ExpiryDate { get; set; }

    [ForeignKey("TranslationId")]
    [InverseProperty("Outputs")]
    public virtual Translation Translation { get; set; } = null!;
}
