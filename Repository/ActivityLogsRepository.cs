using MongoDB.Driver;
using NavioBackend.Interfaces;
using NavioBackend.Models;

namespace NavioBackend.Repository
{
    public class ActivityLogsRepository : IActivityLogsRepository
    {
        private readonly IMongoCollection<ActivityLog> _collection;

        public ActivityLogsRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<ActivityLog>("activityLogs");
        }

        public async Task CreateAsync(ActivityLog log)
        {
            await _collection.InsertOneAsync(log);
        }

        public async Task<List<ActivityLog>> GetAllAsync()
        {
            return await _collection
                .Find(FilterDefinition<ActivityLog>.Empty)
                .SortByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task ClearAllAsync()
        {
            await _collection.DeleteManyAsync(FilterDefinition<ActivityLog>.Empty);
        }
    }
}
