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
    [Route("api/cargo-types")]
    public class CargoTypesController : ControllerBase
    {
        private readonly ICargoTypeRepository _repo;
        private readonly IActivityLogsRepository _logsRepo;

        public CargoTypesController(
            ICargoTypeRepository repo,
            IActivityLogsRepository logsRepo)
        {
            _repo = repo;
            _logsRepo = logsRepo;
        }

        // -------------------------
        // GET
        // -------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _repo.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest("Invalid ID format");

            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        // -------------------------
        // CREATE
        // -------------------------
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CargoTypeCreateDto dto)
        {
            if (dto == null) return BadRequest("Invalid payload");

            var ct = new CargoType
            {
                Name = dto.Name,
                Risk = dto.Risk ?? "Low",
                Color = dto.Color ?? "green",
                Active = dto.Active,
                Count = dto.Count ?? 0
            };

            var created = await _repo.CreateAsync(ct);

            // ---- LOG CREATE ----
            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "cargo type creation",
                    EntityType = "cargo type",
                    EntityId = created.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) created cargo type {created.Name} : id({created.Id})"
                });
            }

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // -------------------------
        // UPDATE
        // -------------------------
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] CargoTypeUpdateDto dto)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest("Invalid ID format");

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.Name = dto.Name ?? existing.Name;
            existing.Risk = dto.Risk ?? existing.Risk;
            existing.Color = dto.Color ?? existing.Color;
            existing.Active = dto.Active ?? existing.Active;
            if (dto.Count.HasValue) existing.Count = dto.Count.Value;

            var ok = await _repo.UpdateAsync(id, existing);
            if (!ok) return BadRequest("Update failed");

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
                    Action = "cargo type update",
                    EntityType = "cargo type",
                    EntityId = refreshed!.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) updated cargo type {refreshed.Name} : id({refreshed.Id})"
                });
            }

            return Ok(refreshed);
        }

        // -------------------------
        // DELETE
        // -------------------------
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest("Invalid ID format");

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            var ok = await _repo.DeleteAsync(id);
            if (!ok) return NotFound();

            // ---- LOG DELETE ----
            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "cargo type delete",
                    EntityType = "cargo type",
                    EntityId = existing.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) deleted cargo type {existing.Name} : id({existing.Id})"
                });
            }

            return Ok(new { message = "Cargo type deleted" });
        }
    }
}
