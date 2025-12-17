using AI_Voice_Translator_SaaS.Data;
using AI_Voice_Translator_SaaS.Interfaces;
using AI_Voice_Translator_SaaS.Models;
using AI_Voice_Translator_SaaS.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AI_Voice_Translator_SaaS.Services
{
    public class AuthService : IAuthService
    {
        private readonly AivoiceTranslatorContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IAuditService _auditService;

        public AuthService(AivoiceTranslatorContext context, IAuditService auditService)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _auditService = auditService;
        }

        public async Task<(bool Success, string Message, User User)> RegisterAsync(RegisterViewModel model)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingUser != null)
            {
                return (false, "Email đã tồn tại", null);
            }

            var user = new User
            {
                Email = model.Email,
                DisplayName = model.DisplayName,
                Role = "User",
                SubscriptionTier = "Free",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(user.Id, "Register");

            return (true, "Đăng ký thành công", user);
        }

        public async Task<(bool Success, string Message, User User)> LoginAsync(LoginViewModel model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                return (false, "Email hoặc mật khẩu không đúng", null);
            }

            if (!user.IsActive)
            {
                return (false, "Tài khoản đã bị khóa", null);
            }

            // Verify password
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return (false, "Email hoặc mật khẩu không đúng", null);
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(user.Id, "Login");

            return (true, "Đăng nhập thành công", user);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> ValidatePasswordAsync(User user, string password)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            return result != PasswordVerificationResult.Failed;
        }
    }
}