////using MongoDB.Driver;
////using NavioBackend.Interfaces;
////using NavioBackend.Models;
////using NavioBackend.Services;

////namespace NavioBackend.Repository
////{
////    public class UserRepository : IUserRepository
////    {
////        private readonly IMongoCollection<User> _users;

////        public UserRepository(DatabaseSettings settings)
////        {
////            var client = new MongoClient(settings.ConnectionString);
////            var db = client.GetDatabase(settings.DatabaseName);
////            _users = db.GetCollection<User>("Users");
////        }

////        public async Task<User?> GetByIdentifierAsync(string identifier)
////        {
////            identifier = identifier.ToLower();

////            return await _users
////                .Find(u =>
////                    u.Email.ToLower() == identifier)
////                .FirstOrDefaultAsync();
////        }

////    }
////}


//using MongoDB.Bson;
//using MongoDB.Driver;
//using NavioBackend.Models;
//using System.Security.Cryptography;
//using System.Text;
//using NavioBackend.Interfaces;

//namespace NavioBackend.Repository
//{
//    public class UserRepository : IUserRepository
//    {
//        private readonly IMongoCollection<User> _users;

//        public UserRepository(IMongoDatabase database)
//        {
//            _users = database.GetCollection<User>("Users");
//        }

//        // ------------------------------ PASSWORD HASHING ------------------------------
//        private string Hash(string input)
//        {
//            using var sha = SHA256.Create();
//            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(input))).ToLower();
//        }

//        // ------------------------------ GET ALL USERS ------------------------------
//        public async Task<List<User>> GetAllAsync() =>
//            await _users.Find(_ => true).ToListAsync();

//        // ------------------------------ GET USERS BY ROLE ------------------------------
//        public async Task<List<User>> GetByRoleAsync(string role)
//        {
//            return await _users
//                .Find(u => u.Role.ToLower() == role.ToLower())
//                .ToListAsync();
//        }

//        // ------------------------------ GET USER BY ID ------------------------------
//        public async Task<User?> GetByIdAsync(string id)
//        {
//            if (!ObjectId.TryParse(id, out _))
//                return null;

//            return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
//        }

//        // ------------------------------ GET USER BY EMAIL ------------------------------
//        public async Task<User?> GetByEmailAsync(string email)
//        {
//            return await _users
//                .Find(u => u.Email.ToLower() == email.ToLower())
//                .FirstOrDefaultAsync();
//        }

//        // ------------------------------ GET BY IDENTIFIER (Email login) ------------------------------
//        public async Task<User?> GetByIdentifierAsync(string identifier)
//        {
//            identifier = identifier.ToLower();

//            return await _users
//                .Find(u => u.Email.ToLower() == identifier)
//                .FirstOrDefaultAsync();
//        }

//        // ------------------------------ GENERATE DRIVER CODE ------------------------------
//        private async Task<string> GenerateDriverCodeAsync()
//        {
//            var last = await _users
//                .Find(u => u.Role == "Driver")
//                .SortByDescending(u => u.DriverId)
//                .FirstOrDefaultAsync();

//            if (last == null || string.IsNullOrWhiteSpace(last.DriverId))
//                return "DRV-001";

//            int num = int.Parse(last.DriverId.Split('-')[1]);
//            return $"DRV-{(num + 1).ToString("000")}";
//        }

//        // ------------------------------ CREATE USER ------------------------------
//        public async Task<User> CreateAsync(User user)
//        {
//            user.Id = null;

//            if (user.Role == "Driver")
//            {
//                // Driver-specific setup
//                user.DriverId = await GenerateDriverCodeAsync();
//                user.LastLogin = DateTime.UtcNow;
//                user.PasswordHash = Hash("d"); // default driver password
//            }
//            else
//            {
//                // Admin / Fleet Manager default password
//                if (string.IsNullOrWhiteSpace(user.PasswordHash))
//                    user.PasswordHash = Hash("admin123");
//            }

//            await _users.InsertOneAsync(user);
//            return user;
//        }

//        // ------------------------------ UPDATE USER ------------------------------
//        public async Task<bool> UpdateAsync(string id, User incoming)
//        {
//            if (!ObjectId.TryParse(id, out _))
//                return false;

//            var existing = await GetByIdAsync(id);
//            if (existing == null)
//                return false;

//            // Merge updatable fields
//            existing.FullName = incoming.FullName ?? existing.FullName;
//            existing.Email = incoming.Email ?? existing.Email;
//            existing.Phone = incoming.Phone ?? existing.Phone;
//            existing.Status = incoming.Status ?? existing.Status;
//            existing.Role = incoming.Role ?? existing.Role;

//            if (!string.IsNullOrWhiteSpace(incoming.Truck))
//                existing.Truck = incoming.Truck;

//            // If password is updated → hash it
//            if (!string.IsNullOrWhiteSpace(incoming.PasswordHash))
//                existing.PasswordHash = Hash(incoming.PasswordHash);

//            await _users.ReplaceOneAsync(u => u.Id == id, existing);
//            return true;
//        }

//        // ------------------------------ DELETE USER ------------------------------
//        public async Task<bool> DeleteAsync(string id)
//        {
//            if (!ObjectId.TryParse(id, out _))
//                return false;

//            var result = await _users.DeleteOneAsync(u => u.Id == id);
//            return result.DeletedCount > 0;
//        }
//    }
//}

using MongoDB.Bson;
using MongoDB.Driver;
using NavioBackend.Interfaces;
using NavioBackend.Models;
using System.Security.Cryptography;
using System.Text;

namespace NavioBackend.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _users;

        public UserRepository(IMongoDatabase database)
        {
            _users = database.GetCollection<User>("Users");
        }

        // ========================================================================
        // PASSWORD HASHING
        // ========================================================================
        private string Hash(string input)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(input)))
                         .ToLower();
        }

        // ========================================================================
        // COMMON CRUD
        // ========================================================================
        public async Task<List<User>> GetAllAsync() =>
            await _users.Find(_ => true).ToListAsync();

        public async Task<List<User>> GetByRoleAsync(string role) =>
            await _users.Find(u => u.Role.ToLower() == role.ToLower()).ToListAsync();

        public async Task<User?> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _)) return null;
            return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _users
                .Find(u => u.Email.ToLower() == email.ToLower())
                .FirstOrDefaultAsync();
        }

        // ========================================================================
        // LOGIN HELPER
        // Email only (identifier = email)
        // ========================================================================
        public async Task<User?> GetByIdentifierAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return null;

            return await _users
                .Find(u => u.Email.ToLower() == identifier.ToLower())
                .FirstOrDefaultAsync();
        }

        // ========================================================================
        // DRIVER AUTO-ID GENERATOR
        // ========================================================================
        private async Task<string> GenerateDriverCodeAsync()
        {
            var last = await _users
                .Find(u => u.Role == "Driver" && u.DriverId != null)
                .SortByDescending(u => u.DriverId)
                .FirstOrDefaultAsync();

            if (last == null || string.IsNullOrWhiteSpace(last.DriverId))
                return "DRV-001";

            int num = int.Parse(last.DriverId.Split('-')[1]);
            return $"DRV-{(num + 1).ToString("000")}";
        }

        // ========================================================================
        // CREATE USER (Admin, Driver, Fleet Manager)
        // ========================================================================
        public async Task<User> CreateAsync(User user)
        {
            user.Id = null;

            if (user.Role == "Driver")
            {
                // Auto-loading driver defaults
                user.DriverId = await GenerateDriverCodeAsync();
                user.LastLogin = DateTime.UtcNow;

                // default driver password → "d"
                user.PasswordHash = Hash("driver123");

                user.AssignedFleetManagerId = user.AssignedFleetManagerId ?? null;
                user.Truck = null;
            }
            else if (user.Role == "FleetManager")
            {
                // FM: No default assignments yet
                user.AssignedDriverIds = new List<string>();
                user.AssignedTruckIds = new List<string>();

                // Default FM password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("fleetmanager123");

            }
            else if (user.Role == "Admin")
            {
                user.PasswordHash = string.IsNullOrWhiteSpace(user.PasswordHash)
                    ? Hash("admin123")
                    : Hash(user.PasswordHash);
            }

            await _users.InsertOneAsync(user);
            return user;
        }

        // ========================================================================
        // UPDATE USER
        // ========================================================================
        public async Task<bool> UpdateAsync(string id, User incoming)
        {
            if (!ObjectId.TryParse(id, out _))
                return false;

            var existing = await GetByIdAsync(id);
            if (existing == null)
                return false;

            // COMMON FIELDS
            existing.FullName = incoming.FullName ?? existing.FullName;
            existing.Email = incoming.Email ?? existing.Email;
            existing.Phone = incoming.Phone ?? existing.Phone;
            existing.Status = incoming.Status ?? existing.Status;

            // 🔐 PASSWORD UPDATE (FIX)
            if (!string.IsNullOrWhiteSpace(incoming.PasswordHash))
            {
                // Hash new password
                using var sha = SHA256.Create();
                existing.PasswordHash = Convert.ToHexString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(incoming.PasswordHash))
                ).ToLower();
            }

            // DRIVER FIELDS
            if (existing.Role == "Driver")
            {
                existing.Truck = incoming.Truck ?? existing.Truck;
                existing.AssignedFleetManagerId = incoming.AssignedFleetManagerId ?? existing.AssignedFleetManagerId;
            }

            // FLEET MANAGER FIELDS
            if (existing.Role == "FleetManager")
            {
                existing.AssignedDriverIds = incoming.AssignedDriverIds ?? existing.AssignedDriverIds;
                existing.AssignedTruckIds = incoming.AssignedTruckIds ?? existing.AssignedTruckIds;
            }

            await _users.ReplaceOneAsync(u => u.Id == id, existing);
            return true;
        }

        // ========================================================================
        // DELETE USER
        // ========================================================================
        public async Task<bool> DeleteAsync(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return false;

            var result = await _users.DeleteOneAsync(u => u.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task ClearAssignedFleetManagerAsync(string userId)
        {
            if (!ObjectId.TryParse(userId, out _))
                return;

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.AssignedFleetManagerId, null);

            await _users.UpdateOneAsync(filter, update);
        }

    }
}
