using AI_Voice_Translator_SaaS.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace AI_Voice_Translator_SaaS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<AudioFile> AudioFiles { get; set; }
        public DbSet<Transcript> Transcripts { get; set; }
        public DbSet<Translation> Translations { get; set; }
        public DbSet<Output> Outputs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20).HasDefaultValue("User");
                entity.Property(e => e.SubscriptionTier).IsRequired().HasMaxLength(20).HasDefaultValue("Free");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // AudioFile Configuration
            modelBuilder.Entity<AudioFile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.OriginalFileUrl).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
                entity.Property(e => e.UploadedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.AudioFiles)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Transcript Configuration
            modelBuilder.Entity<Transcript>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.AudioFileId);
                entity.Property(e => e.OriginalText).IsRequired();
                entity.Property(e => e.DetectedLanguage).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Confidence).HasPrecision(5, 2);
                entity.Property(e => e.ProcessedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.AudioFile)
                    .WithOne(a => a.Transcript)
                    .HasForeignKey<Transcript>(e => e.AudioFileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Translation Configuration
            modelBuilder.Entity<Translation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TranscriptId);
                entity.Property(e => e.TargetLanguage).IsRequired().HasMaxLength(10);
                entity.Property(e => e.TranslatedText).IsRequired();
                entity.Property(e => e.TranslationEngine).IsRequired().HasMaxLength(50).HasDefaultValue("Gemini");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Transcript)
                    .WithMany(t => t.Translations)
                    .HasForeignKey(e => e.TranscriptId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Output Configuration
            modelBuilder.Entity<Output>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TranslationId);
                entity.Property(e => e.OutputFileUrl).IsRequired().HasMaxLength(500);
                entity.Property(e => e.VoiceType).IsRequired().HasMaxLength(20).HasDefaultValue("Female");
                entity.Property(e => e.VoiceModel).IsRequired().HasMaxLength(50).HasDefaultValue("Google");
                entity.Property(e => e.GeneratedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.DownloadCount).HasDefaultValue(0);

                entity.HasOne(e => e.Translation)
                    .WithOne(t => t.Output)
                    .HasForeignKey<Output>(e => e.TranslationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AuditLog Configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Timestamp);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.AuditLogs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Seed Data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var adminId = Guid.NewGuid();
            var testUserId = Guid.NewGuid();

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = adminId,
                    Email = "admin@aivt.com",
                    DisplayName = "Admin User",
                    Role = "Admin",
                    SubscriptionTier = "Premium",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    //Password will be set via UserManager after migration
                },
                new User
                {
                    Id = testUserId,
                    Email = "test@user.com",
                    DisplayName = "Test User",
                    Role = "User",
                    SubscriptionTier = "Free",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                }
            );
        }
    }
}
