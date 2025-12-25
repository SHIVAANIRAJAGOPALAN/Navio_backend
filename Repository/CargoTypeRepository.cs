using MongoDB.Bson;
using MongoDB.Driver;
using NavioBackend.Interfaces;
using NavioBackend.Models;

namespace NavioBackend.Repository
{
    public class CargoTypeRepository : ICargoTypeRepository
    {
        private readonly IMongoCollection<CargoType> _collection;

        public CargoTypeRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<CargoType>("CargoTypes");
        }

        public async Task<List<CargoType>> GetAllAsync() =>
            await _collection.Find(_ => true).ToListAsync();

        public async Task<CargoType?> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _)) return null;
            return await _collection.Find(c => c.Id == id).FirstOrDefaultAsync();
        }

        public async Task<CargoType> CreateAsync(CargoType cargoType)
        {
            cargoType.Id = null; // let MongoDB generate
            cargoType.Count = cargoType.Count; // keep whatever was provided or default
            await _collection.InsertOneAsync(cargoType);
            return cargoType;
        }

        public async Task<bool> UpdateAsync(string id, CargoType incoming)
        {
            if (!ObjectId.TryParse(id, out _)) return false;

            var existing = await GetByIdAsync(id);
            if (existing == null) return false;

            // Partial merge
            existing.Name = incoming.Name ?? existing.Name;
            existing.Risk = incoming.Risk ?? existing.Risk;
            existing.Color = incoming.Color ?? existing.Color;
            existing.Active = incoming.Active;
            if (incoming.Count != 0) existing.Count = incoming.Count;

            var res = await _collection.ReplaceOneAsync(c => c.Id == id, existing);
            return res.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _)) return false;
            var res = await _collection.DeleteOneAsync(c => c.Id == id);
            return res.DeletedCount > 0;
        }
    }
}

