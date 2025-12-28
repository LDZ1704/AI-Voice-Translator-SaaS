using AI_Voice_Translator_SaaS.Models;
using AI_Voice_Translator_SaaS.Models.ViewModels;

namespace AI_Voice_Translator_SaaS.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, User User)> RegisterAsync(RegisterViewModel model);
        Task<(bool Success, string Message, User User)> LoginAsync(LoginViewModel model);
        Task<User> GetUserByEmailAsync(string email);
        Task<bool> ValidatePasswordAsync(User user, string password);
        Task<User> GetOrCreateOAuthUserAsync(string provider, string providerKey, string email, string displayName);
    }
}