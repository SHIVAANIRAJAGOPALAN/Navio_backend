using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using NavioBackend.DTOs;
using NavioBackend.Interfaces;
using NavioBackend.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace NavioBackend.Controllers
{
    [ApiController]
    [Route("api/fleet")]
    public class FleetController : ControllerBase
    {
        private readonly ITruckRepository _repo;
        private readonly IActivityLogsRepository _logsRepo;

        public FleetController(
            ITruckRepository repo,
            IActivityLogsRepository logsRepo)
        {
            _repo = repo;
            _logsRepo = logsRepo;
        }

        // --------------------------------------------------
        // GET ALL FLEET
        // --------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var trucks = await _repo.GetAllAsync();
            return Ok(trucks);
        }

        [HttpGet("by-manager/{fleetManagerId}")]
        public async Task<IActionResult> GetByFleetManager(string fleetManagerId)
        {
            var trucks = await _repo.GetByFleetManagerAsync(fleetManagerId);
            return Ok(trucks);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest("Invalid ID");

            var truck = await _repo.GetByIdAsync(id);
            return truck == null ? NotFound() : Ok(truck);
        }

        // --------------------------------------------------
        // CREATE FLEET
        // --------------------------------------------------
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] TruckCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Truck data is required");

            var truck = new Truck
            {
                TruckNumber = dto.Number,
                Length = dto.Length,
                Height = dto.Height,
                Width = dto.Width,
                Capacity = dto.Capacity,
                CapacityUnit = dto.CapacityUnit ?? "lbs",
                Status = dto.Status ?? "Available",
                BodyType = dto.BodyType,
                DutyClass = dto.DutyClass
            };

            await _repo.CreateAsync(truck);

            // ---- LOG CREATE ----
            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "fleet create",
                    EntityType = "fleet",
                    EntityId = truck.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) created fleet truck {truck.TruckNumber} : id({truck.Id})"
                });
            }

            return Ok(truck);
        }

        // --------------------------------------------------
        // UPDATE FLEET
        // --------------------------------------------------
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] TruckCreateDto dto)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest("Invalid ID format");

            if (dto == null)
                return BadRequest("Truck data is required");

            var existingTruck = await _repo.GetByIdAsync(id);
            if (existingTruck == null)
                return NotFound($"Truck with ID {id} not found");

            var truck = new Truck
            {
                Id = id,
                TruckNumber = dto.Number,
                Length = dto.Length,
                Height = dto.Height,
                Width = dto.Width,
                Capacity = dto.Capacity,
                CapacityUnit = dto.CapacityUnit ?? "lbs",
                Status = dto.Status ?? "Available",
                BodyType = dto.BodyType,
                DutyClass = dto.DutyClass,
                AssignedFleetManagerId = existingTruck.AssignedFleetManagerId,
                AssignedDriverId = existingTruck.AssignedDriverId
            };

            await _repo.UpdateAsync(id, truck);

            // ---- LOG UPDATE ----
            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "fleet update",
                    EntityType = "fleet",
                    EntityId = truck.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) updated fleet truck {truck.TruckNumber} : id({truck.Id})"
                });
            }

            return Ok(truck);
        }

        // --------------------------------------------------
        // DELETE FLEET
        // --------------------------------------------------
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest("Invalid ID format");

            var existingTruck = await _repo.GetByIdAsync(id);
            if (existingTruck == null)
                return NotFound();

            await _repo.DeleteAsync(id);

            // ---- LOG DELETE ----
            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "fleet delete",
                    EntityType = "fleet",
                    EntityId = existingTruck.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) deleted fleet truck {existingTruck.TruckNumber} : id({existingTruck.Id})"
                });
            }

            return Ok(true);
        }
    }
}
