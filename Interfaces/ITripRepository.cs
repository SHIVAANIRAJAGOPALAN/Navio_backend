using NavioBackend.Models;

namespace NavioBackend.Interfaces
{
    public interface ITripRepository
    {
        Task<IEnumerable<Trip>> GetAll();
        Task<Trip> GetById(string id);
        Task<IEnumerable<Trip>> GetByFleetManager(string fleetManagerId);
        Task<IEnumerable<Trip>> GetByDriver(string driverId);
        Task<IEnumerable<Trip>> GetUnassigned(string fleetManagerId);
        Task Create(Trip trip);
        Task Update(string id, Trip trip);
        Task Delete(string id);
        Task UpdateStatus(string id, string status);
        Task UpdateProgress(string id, Trip progress);
        Task UpdateCancellationReason(string id, string reason);

    }
}
