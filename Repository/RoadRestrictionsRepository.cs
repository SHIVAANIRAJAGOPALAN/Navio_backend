using MongoDB.Driver;
using NavioBackend.Interfaces;
using NavioBackend.Models;

namespace NavioBackend.Repository
{
    public class RoadRestrictionsRepository : IRoadRestrictionsRepository
    {
        private readonly IMongoCollection<RoadRestriction> _collection;

        public RoadRestrictionsRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<RoadRestriction>("RoadRestrictions");
        }

        // ---------------- CREATE (SINGLE) ----------------
        public async Task<RoadRestriction> Create(RoadRestriction restriction)
        {
            await _collection.InsertOneAsync(restriction);
            return restriction;
        }

        // ---------------- CREATE (BULK) ----------------
        public async Task<List<RoadRestriction>> BulkCreate(
            List<long> roadIds,
            List<string> issues,
            DateTime dateTime
        )
        {
            var restrictions = roadIds.Select(roadId => new RoadRestriction
            {
                RoadId = roadId,
                Issues = issues,
                DateTime = dateTime
            }).ToList();

            if (restrictions.Count > 0)
            {
                await _collection.InsertManyAsync(restrictions);
            }

            return restrictions;
        }

        // ---------------- READ ----------------
        public async Task<List<RoadRestriction>> GetAll()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<List<RoadRestriction>> GetByDate(
            DateTime? startDate,
            DateTime? endDate
        )
        {
            var builder = Builders<RoadRestriction>.Filter;
            var filters = new List<FilterDefinition<RoadRestriction>>();

            if (startDate.HasValue)
                filters.Add(builder.Gte(r => r.DateTime, startDate.Value));

            if (endDate.HasValue)
                filters.Add(builder.Lte(r => r.DateTime, endDate.Value));

            var finalFilter = filters.Count > 0
                ? builder.And(filters)
                : builder.Empty;

            return await _collection.Find(finalFilter).ToListAsync();
        }
    }
}
