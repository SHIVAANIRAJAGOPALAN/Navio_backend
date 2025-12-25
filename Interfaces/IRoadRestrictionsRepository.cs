using NavioBackend.Models;

namespace NavioBackend.Interfaces
{
    public interface IRoadRestrictionsRepository
    {
        // CREATE
        Task<RoadRestriction> Create(RoadRestriction restriction);
        Task<List<RoadRestriction>> BulkCreate(
            List<long> roadIds,
            List<string> issues,
            DateTime dateTime
        );

        // READ
        Task<List<RoadRestriction>> GetAll();
        Task<List<RoadRestriction>> GetByDate(
            DateTime? startDate,
            DateTime? endDate
        );
    }
}
