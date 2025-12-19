using System;
using System.Collections.Generic;
using AI_Voice_Translator_SaaS.Models;
using Microsoft.EntityFrameworkCore;

namespace AI_Voice_Translator_SaaS.Data;

public partial class AivoiceTranslatorContext : DbContext
{
    public AivoiceTranslatorContext()
    {
    }

    public AivoiceTranslatorContext(DbContextOptions<AivoiceTranslatorContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AudioFile> AudioFiles { get; set; }
    public virtual DbSet<AuditLog> AuditLogs { get; set; }
    public virtual DbSet<Output> Outputs { get; set; }
    public virtual DbSet<Transcript> Transcripts { get; set; }
    public virtual DbSet<Translation> Translations { get; set; }
    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=LAM-LAPTOP;Database=AIVoiceTranslator;Integrated Security=True;TrustServerCertificate=True");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Role).HasDefaultValue("User");
            entity.Property(e => e.SubscriptionTier).HasDefaultValue("Free");
            entity.Property(e => e.SubscriptionExpiryDate).IsRequired(false);
        });

        modelBuilder.Entity<AudioFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.User)
                .WithMany(p => p.AudioFiles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transcript>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ProcessedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.AudioFile)
                .WithOne(p => p.Transcripts)
                .HasForeignKey<Transcript>(d => d.AudioFileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Translation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.TranslationEngine).HasDefaultValue("Gemini");

            entity.HasOne(d => d.Transcript)
                .WithMany(p => p.Translations)
                .HasForeignKey(d => d.TranscriptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Output>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.GeneratedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.VoiceModel).HasDefaultValue("Google");
            entity.Property(e => e.VoiceType).HasDefaultValue("Female");

            entity.HasOne(d => d.Translation)
                .WithOne(p => p.Outputs)
                .HasForeignKey<Output>(d => d.TranslationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.User)
                .WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}