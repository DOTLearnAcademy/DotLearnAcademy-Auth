using DotLearn.Auth.Data;
using DotLearn.Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotLearn.Auth.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserDbContext _context;

    public UserRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _context.Users.FindAsync(id);

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken) =>
        await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

    public async Task<User?> GetByPasswordResetTokenAsync(string token) =>
        await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);

    public async Task<User?> GetByOAuthSubjectAsync(string provider, string subject) =>
        await _context.Users.FirstOrDefaultAsync(u => 
            u.AuthProvider == provider && u.GoogleSubjectId == subject);

    public async Task<bool> EmailExistsAsync(string email) =>
        await _context.Users.AnyAsync(u => u.Email == email);

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync(string? query, string? role)
    {
        var q = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lowercaseQuery = query.ToLower();
            q = q.Where(u => u.Email.ToLower().Contains(lowercaseQuery) || u.FullName.ToLower().Contains(lowercaseQuery));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            q = q.Where(u => u.Role == role);
        }

        return await q.OrderByDescending(u => u.CreatedAt).ToListAsync();
    }

    public async Task<int> GetActiveAdminCountAsync()
    {
        return await _context.Users.CountAsync(u => u.Role == "Admin" && u.IsActive && !u.IsDeleted);
    }
}
