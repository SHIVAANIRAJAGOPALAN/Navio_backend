using NavioBackend.Models;

namespace NavioBackend.Interfaces
{
    public interface IUserRepository
    {
        // Fetch all users (Admins, Drivers, Fleet Managers)
        Task<List<User>> GetAllAsync();

        // Fetch users by specific role
        Task<List<User>> GetByRoleAsync(string role);

        // Get user by Mongo ObjectId
        Task<User?> GetByIdAsync(string id);

        // Get user by email
        Task<User?> GetByEmailAsync(string email);

        Task<User?> GetByIdentifierAsync(string identifier);

        // Create new user (Admin, Driver, Fleet Manager)
        Task<User> CreateAsync(User user);

        // Update an existing user
        Task<bool> UpdateAsync(string id, User user);

        // Delete user by ID
        Task<bool> DeleteAsync(string id);

        Task ClearAssignedFleetManagerAsync(string userId);

    }
}


