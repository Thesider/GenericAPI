using GenericAPI.Models;

namespace GenericAPI.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> IsEmailUniqueAsync(string email);
    Task<bool> IsUsernameUniqueAsync(string username);    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
    Task<bool> UpdateUserStatusAsync(int userId, bool isActive);
    Task<bool> ChangePasswordAsync(int userId, string newPasswordHash);
}
