using NavioBackend.Models;

namespace NavioBackend.Interfaces
{
    public interface ICargoTypeRepository
    {
        Task<List<CargoType>> GetAllAsync();
        Task<CargoType?> GetByIdAsync(string id);
        Task<CargoType> CreateAsync(CargoType cargoType);
        Task<bool> UpdateAsync(string id, CargoType cargoType);
        Task<bool> DeleteAsync(string id);
    }
}

