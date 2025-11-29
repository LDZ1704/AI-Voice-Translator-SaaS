using AI_Voice_Translator_SaaS.Models;

namespace AI_Voice_Translator_SaaS.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetActiveUsersAsync();
        Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
        Task<int> GetTotalUsersCountAsync();
    }
}