using MongoDB.Driver;
using NavioBackend.Interfaces;
using NavioBackend.Models;

namespace NavioBackend.Repository
{
    public class DriverNotesRepository : IDriverNotesRepository
    {
        private readonly IMongoCollection<DriverNote> _collection;

        public DriverNotesRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<DriverNote>("DriverNotes");
        }

        // ---------------- CREATE ----------------
        public async Task<DriverNote> Create(DriverNote note)
        {
            await _collection.InsertOneAsync(note);
            return note;
        }

        // ---------------- READ ----------------
        public async Task<List<DriverNote>> GetAll()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<DriverNote?> GetById(string id)
        {
            return await _collection.Find(n => n.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<DriverNote>> GetByDriverId(string driverId)
        {
            return await _collection
                .Find(n => n.DriverId == driverId)
                .ToListAsync();
        }

        public async Task<List<DriverNote>> GetWithIssuesFaced()
        {
            return await _collection
                .Find(n => n.IssueFaced == true)
                .ToListAsync();
        }

        // ---------------- UPDATE ----------------
        public async Task<DriverNote?> Update(string id, DriverNote updatedNote)
        {
            updatedNote.Id = id;

            var result = await _collection.ReplaceOneAsync(
                n => n.Id == id,
                updatedNote
            );

            return result.ModifiedCount > 0 ? updatedNote : null;
        }

        // ---------------- DELETE ----------------
        public async Task<DriverNote?> Delete(string id)
        {
            return await _collection.FindOneAndDeleteAsync(
                n => n.Id == id
            );
        }

        // ---------------- APPROVAL ----------------
        public async Task<DriverNote?> Approve(string id)
        {
            var update = Builders<DriverNote>.Update
                .Set(n => n.Status, "approved");

            return await _collection.FindOneAndUpdateAsync(
                n => n.Id == id,
                update,
                new FindOneAndUpdateOptions<DriverNote>
                {
                    ReturnDocument = ReturnDocument.After
                }
            );
        }

        public async Task<List<DriverNote>> BulkApprove(List<string> ids)
        {
            var filter = Builders<DriverNote>.Filter.In(n => n.Id, ids);
            var update = Builders<DriverNote>.Update
                .Set(n => n.Status, "approved");

            await _collection.UpdateManyAsync(filter, update);

            return await _collection
                .Find(n => ids.Contains(n.Id))
                .ToListAsync();
        }
    }
}
