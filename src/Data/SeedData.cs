using AI_Voice_Translator_SaaS.Data;
using AI_Voice_Translator_SaaS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIVoiceTranslator.Data
{
    public static class SeedData
    {
        public static async Task Initialize(AivoiceTranslatorContext context)
        {
            await context.Database.MigrateAsync();

            if (context.Users.Any())
            {
                return;
            }

            var passwordHasher = new PasswordHasher<User>();

            var admin = new User
            {
                Email = "admin@aivt.com",
                DisplayName = "Admin User",
                Role = "Admin",
                SubscriptionTier = "Premium",
                IsActive = true
            };
            admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin@123");

            var testUser = new User
            {
                Email = "test@user.com",
                DisplayName = "Test User",
                Role = "User",
                SubscriptionTier = "Free"
            };
            testUser.PasswordHash = passwordHasher.HashPassword(testUser, "Test@123");

            context.Users.AddRange(admin, testUser);
            await context.SaveChangesAsync();

            Console.WriteLine("Database seeded!");
            Console.WriteLine("Admin: admin@aivt.com / Admin@123");
            Console.WriteLine("User: test@user.com / Test@123");
        }
    }
}