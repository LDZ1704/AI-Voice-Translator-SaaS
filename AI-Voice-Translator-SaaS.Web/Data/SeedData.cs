using AI_Voice_Translator_SaaS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AI_Voice_Translator_SaaS.Data
{
    public static class SeedData
    {
        public static async Task Initialize(ApplicationDbContext context)
        {
            // Ensure database is created
            await context.Database.MigrateAsync();

            // Check if users already exist
            if (context.Users.Any())
            {
                return;
            }

            var passwordHasher = new PasswordHasher<User>();

            // Create Admin User
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@aivt.com",
                DisplayName = "Admin User",
                Role = "Admin",
                SubscriptionTier = "Premium",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin@123");

            // Create Test User
            var testUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@user.com",
                DisplayName = "Test User",
                Role = "User",
                SubscriptionTier = "Free",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            testUser.PasswordHash = passwordHasher.HashPassword(testUser, "Test@123");

            // Add users to database
            context.Users.AddRange(admin, testUser);
            await context.SaveChangesAsync();

            // Create Sample Audio File (for testing)
            var sampleAudioFile = new AudioFile
            {
                Id = Guid.NewGuid(),
                UserId = testUser.Id,
                FileName = "sample_welcome.mp3",
                OriginalFileUrl = "/uploads/sample_welcome.mp3",
                FileSizeBytes = 1024000, // 1MB
                DurationSeconds = 180, // 3 minutes
                Status = "Completed",
                UploadedAt = DateTime.UtcNow.AddDays(-1)
            };

            context.AudioFiles.Add(sampleAudioFile);
            await context.SaveChangesAsync();

            // Create Sample Transcript
            var sampleTranscript = new Transcript
            {
                Id = Guid.NewGuid(),
                AudioFileId = sampleAudioFile.Id,
                OriginalText = "Welcome to AI Voice Translator. This is a sample audio file for testing purposes.",
                DetectedLanguage = "en",
                Confidence = 95.5m,
                ProcessedAt = DateTime.UtcNow.AddDays(-1)
            };

            context.Transcripts.Add(sampleTranscript);
            await context.SaveChangesAsync();

            // Create Sample Translation
            var sampleTranslation = new Translation
            {
                Id = Guid.NewGuid(),
                TranscriptId = sampleTranscript.Id,
                TargetLanguage = "vi",
                TranslatedText = "Chào mừng đến với AI Voice Translator. Đây là file âm thanh mẫu để kiểm thử.",
                TranslationEngine = "Gemini",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UserRating = 5
            };

            context.Translations.Add(sampleTranslation);
            await context.SaveChangesAsync();

            // Create Sample Output
            var sampleOutput = new Output
            {
                Id = Guid.NewGuid(),
                TranslationId = sampleTranslation.Id,
                OutputFileUrl = "/outputs/sample_welcome_vi.mp3",
                VoiceType = "Female",
                VoiceModel = "Google",
                GeneratedAt = DateTime.UtcNow.AddDays(-1),
                ExpiryDate = DateTime.UtcNow.AddDays(29),
                DownloadCount = 3
            };

            context.Outputs.Add(sampleOutput);
            await context.SaveChangesAsync();

            // Create Sample Audit Logs
            var auditLogs = new[]
            {
                new AuditLog
                {
                    UserId = admin.Id,
                    Action = "Login",
                    IpAddress = "127.0.0.1",
                    UserAgent = "Mozilla/5.0",
                    Timestamp = DateTime.UtcNow.AddHours(-2)
                },
                new AuditLog
                {
                    UserId = testUser.Id,
                    Action = "Upload",
                    IpAddress = "127.0.0.1",
                    UserAgent = "Mozilla/5.0",
                    Timestamp = DateTime.UtcNow.AddDays(-1)
                },
                new AuditLog
                {
                    UserId = testUser.Id,
                    Action = "Download",
                    IpAddress = "127.0.0.1",
                    UserAgent = "Mozilla/5.0",
                    Timestamp = DateTime.UtcNow.AddHours(-1)
                }
            };

            context.AuditLogs.AddRange(auditLogs);
            await context.SaveChangesAsync();

            Console.WriteLine("Database seeded successfully!");
            Console.WriteLine($"Admin: admin@aivt.com / Admin@123");
            Console.WriteLine($"User: test@user.com / Test@123");
        }
    }
}
