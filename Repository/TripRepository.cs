using MongoDB.Bson;
using MongoDB.Driver;
using NavioBackend.Interfaces;
using NavioBackend.Models;
using NavioBackend.Services;
using System.Collections.Generic;

namespace NavioBackend.Repository
{
    public class TripRepository : ITripRepository
    {
        private readonly IMongoCollection<Trip> _trips;

        public TripRepository(DatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var db = client.GetDatabase(settings.DatabaseName);
            _trips = db.GetCollection<Trip>("Trips");
        }

        public async Task<IEnumerable<Trip>> GetAll() =>
            await _trips.Find(_ => true).ToListAsync();

        public async Task<Trip> GetById(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return null;

            return await _trips.Find(t => t.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Trip>> GetByFleetManager(string fleetManagerId) =>
            await _trips.Find(t => t.FleetManagerId == fleetManagerId).ToListAsync();

        public async Task<IEnumerable<Trip>> GetByDriver(string driverId) =>
            await _trips.Find(t => t.DriverId == driverId).ToListAsync();

        public async Task<IEnumerable<Trip>> GetUnassigned(string fmId) =>
            await _trips.Find(t => t.FleetManagerId == fmId &&
                                   (t.DriverId == null || t.TruckId == null))
                        .ToListAsync();

        public async Task Create(Trip trip)
        {
            // Ensure repository-level defaults so DB stored documents are consistent
            trip.SpecialInstructions ??= string.Empty;
            trip.RoadRestrictions ??= new List<string>();

            // Distance/duration are non-nullable and default to 0 via model initializers,
            // but guard against negative values if someone passed them explicitly.
            if (trip.DistanceTravelled < 0) trip.DistanceTravelled = 0;
            if (trip.Duration < 0) trip.Duration = 0;


            await _trips.InsertOneAsync(trip);
        }

        public async Task Update(string id, Trip trip) =>
            await _trips.ReplaceOneAsync(t => t.Id == id, trip);

        public async Task Delete(string id) =>
            await _trips.DeleteOneAsync(t => t.Id == id);

        public async Task UpdateStatus(string id, string status)
        {
            var update = Builders<Trip>.Update.Set(t => t.Status, status);
            await _trips.UpdateOneAsync(t => t.Id == id, update);
        }

        public async Task UpdateProgress(string id, Trip progress)
{
    var update = Builders<Trip>.Update
        .Set(t => t.DistanceTravelled, progress.DistanceTravelled)
        .Set(t => t.Duration, progress.Duration);
        // Do NOT update Status

    await _trips.UpdateOneAsync(t => t.Id == id, update);
}

        public async Task UpdateCancellationReason(string id, string reason)
        {
            // defensive: ensure id looks like an ObjectId-like string (optional)
            if (string.IsNullOrWhiteSpace(id)) return;

            var update = Builders<Trip>.Update.Set(t => t.CancellationReason, reason);
            await _trips.UpdateOneAsync(t => t.Id == id, update);
        }


    }
}
