using AIVoiceTranslator.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIVoiceTranslator.Data
{
    public static class SeedData
    {
        public static async Task Initialize(ApplicationDbContext context)
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
                SubscriptionTier = "Premium"
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