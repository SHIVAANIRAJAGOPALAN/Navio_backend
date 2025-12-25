//using NavioBackend.Models;

//namespace NavioBackend.Interfaces
//{
//    public interface ITruckRepository
//    {
//        Task<List<Truck>> GetAllAsync();
//        Task<Truck?> GetByIdAsync(string id);
//        Task<Truck> CreateAsync(Truck truck);
//        Task<bool> UpdateAsync(string id, Truck truck);
//        Task<bool> DeleteAsync(string id);

//        // Assignment helpers
//        Task AssignToFleetManagerAsync(string truckId, string fleetManagerId);
//        Task AssignToDriverAsync(string truckId, string driverId);
//    }
//}

using NavioBackend.Models;

namespace NavioBackend.Interfaces
{
    public interface ITruckRepository
    {
        Task<List<Truck>> GetAllAsync();
        Task<Truck> GetByIdAsync(string id);
        Task<List<Truck>> GetByFleetManagerAsync(string fleetManagerId);
        Task CreateAsync(Truck truck);
        Task UpdateAsync(string id, Truck truck);
        Task DeleteAsync(string id);

        Task AssignToFleetManagerAsync(string truckId, string fleetManagerId);
        Task AssignToDriverAsync(string truckId, string driverId);
    }
}
