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
    [Route("api/[controller]")]
    public class DriversController : ControllerBase
    {
        private readonly IUserRepository _repo;
        private readonly IActivityLogsRepository _logsRepo;

        public DriversController(
            IUserRepository repo,
            IActivityLogsRepository logsRepo)
        {
            _repo = repo;
            _logsRepo = logsRepo;
        }

        // -------------------------------------------------------------------------
        // GET ALL DRIVERS
        // -------------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var drivers = await _repo.GetByRoleAsync("Driver");
            return Ok(drivers);
        }

        // -------------------------------------------------------------------------
        // GET DRIVER BY ID
        // -------------------------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest("Invalid ID format");

            var driver = await _repo.GetByIdAsync(id);
            if (driver == null || driver.Role != "Driver")
                return NotFound();

            return Ok(driver);
        }

        // -------------------------------------------------------------------------
        // CREATE NEW DRIVER
        // -------------------------------------------------------------------------
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] DriverCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid payload");

            var driver = new User
            {
                FullName = dto.FullName,
                //Email = dto.Email,
                Email = dto.Email.Trim().ToLower(),
                Phone = dto.Phone,
                Status = dto.Status ?? "Active",
                Role = "Driver",
                Truck = null,
                AssignedFleetManagerId = null
            };

            var created = await _repo.CreateAsync(driver);

            // ---- LOG CREATE ----
            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "driver create",
                    EntityType = "driver",
                    EntityId = created.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) added driver {created.FullName} : id({created.Id})"
                });
            }

            return Ok(created);
        }

        // -------------------------------------------------------------------------
        // UPDATE DRIVER
        // -------------------------------------------------------------------------
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] DriverUpdateDto dto)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest("Invalid ID format");

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.Role != "Driver")
                return NotFound();

            var incoming = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Status = dto.Status,
            };

            var updated = await _repo.UpdateAsync(id, incoming);
            if (!updated)
                return BadRequest("Update failed");

            var refreshed = await _repo.GetByIdAsync(id);

            // ---- LOG UPDATE ----
            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "driver update",
                    EntityType = "driver",
                    EntityId = refreshed!.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) updated driver {refreshed.FullName} : id({refreshed.Id})"
                });
            }

            return Ok(refreshed);
        }

        // -------------------------------------------------------------------------
        // DELETE DRIVER
        // -------------------------------------------------------------------------
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest("Invalid ID format");

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.Role != "Driver")
                return NotFound();

            var deleted = await _repo.DeleteAsync(id);
            if (!deleted)
                return BadRequest("Delete failed");

            // ---- LOG DELETE ----
            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "driver delete",
                    EntityType = "driver",
                    EntityId = existing.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) deleted driver {existing.FullName} : id({existing.Id})"
                });
            }

            return Ok(new { message = "Driver deleted successfully" });
        }
    }
}
