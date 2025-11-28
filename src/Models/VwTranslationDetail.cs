using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AI_Voice_Translator_SaaS.Models;

[Keyless]
public partial class VwTranslationDetail
{
    public Guid AudioFileId { get; set; }

    [StringLength(255)]
    public string FileName { get; set; } = null!;

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [StringLength(256)]
    public string UserEmail { get; set; } = null!;

    [StringLength(100)]
    public string UserName { get; set; } = null!;

    public string? OriginalText { get; set; }

    [StringLength(10)]
    public string? SourceLanguage { get; set; }

    [StringLength(10)]
    public string? TargetLanguage { get; set; }

    public string? TranslatedText { get; set; }

    public int? UserRating { get; set; }

    [StringLength(500)]
    public string? OutputFileUrl { get; set; }

    [StringLength(20)]
    public string? VoiceType { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime? TranslatedAt { get; set; }
}
