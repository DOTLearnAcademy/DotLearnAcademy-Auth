using DotLearn.Auth.Models.Entities;

namespace DotLearn.Auth.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<User?> GetByPasswordResetTokenAsync(string token);
    Task<User?> GetByOAuthSubjectAsync(string provider, string subject);
    Task<bool> EmailExistsAsync(string email);
    Task<IEnumerable<User>> GetAllUsersAsync(string? query, string? role);
    Task<int> GetActiveAdminCountAsync();
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
