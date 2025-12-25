
using MongoDB.Driver;
using NavioBackend.Interfaces;
using NavioBackend.Models;
using MongoDB.Bson;


namespace NavioBackend.Repository
{
    public class TruckRepository : ITruckRepository
    {
        private readonly IMongoCollection<Truck> _collection;

        public TruckRepository(IMongoDatabase db)
        {
            _collection = db.GetCollection<Truck>("Trucks");
        }

        public async Task<List<Truck>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<Truck> GetByIdAsync(string id)
        {
            return await _collection.Find(t => t.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<Truck>> GetByFleetManagerAsync(string fleetManagerId)
        {
            return await _collection.Find(t => t.AssignedFleetManagerId == fleetManagerId).ToListAsync();
        }

        public async Task CreateAsync(Truck truck)
        {
            // Auto-generate ID if missing
            if (string.IsNullOrWhiteSpace(truck.Id))
                truck.Id = ObjectId.GenerateNewId().ToString();

            await _collection.InsertOneAsync(truck);
        }


        public async Task UpdateAsync(string id, Truck truck)
        {
            var result = await _collection.ReplaceOneAsync(t => t.Id == id, truck);
            if (result.MatchedCount == 0)
                throw new Exception($"Truck with ID {id} not found");
        }

        public async Task DeleteAsync(string id)
        {
            await _collection.DeleteOneAsync(t => t.Id == id);
        }

        public async Task AssignToFleetManagerAsync(string truckId, string fleetManagerId)
        {
            var update = Builders<Truck>.Update
                .Set(t => t.AssignedFleetManagerId, fleetManagerId);

            await _collection.UpdateOneAsync(
                t => t.Id == truckId,
                update
            );
        }

        public async Task AssignToDriverAsync(string truckId, string driverId)
        {
            var update = Builders<Truck>.Update
                .Set(t => t.AssignedDriverId, driverId);

            await _collection.UpdateOneAsync(
                t => t.Id == truckId,
                update
            );
        }

    }
}


