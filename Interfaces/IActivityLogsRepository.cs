using NavioBackend.Models;

namespace NavioBackend.Interfaces
{
    public interface IActivityLogsRepository
    {
        Task CreateAsync(ActivityLog log);
        Task<List<ActivityLog>> GetAllAsync();
        Task ClearAllAsync();
    }
}
