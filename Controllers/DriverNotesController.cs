using Microsoft.AspNetCore.Mvc;
using NavioBackend.Interfaces;
using NavioBackend.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace NavioBackend.Controllers
{
    [ApiController]
    [Route("api/driver-notes")]
    public class DriverNotesController : ControllerBase
    {
        private readonly IDriverNotesRepository _notesRepo;
        private readonly IRoadRestrictionsRepository _restrictionsRepo;
        private readonly IActivityLogsRepository _logsRepo;

        public DriverNotesController(
            IDriverNotesRepository notesRepo,
            IRoadRestrictionsRepository restrictionsRepo,
            IActivityLogsRepository logsRepo)
        {
            _notesRepo = notesRepo;
            _restrictionsRepo = restrictionsRepo;
            _logsRepo = logsRepo;
        }



        // --------------------------------------------------
        // CREATE DRIVER NOTE
        // --------------------------------------------------
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] DriverNote note)
        {
            if (note == null)
                return BadRequest();

            note.CreatedDateTime = DateTime.UtcNow;
            note.DateTime = note.DateTime == default
                ? note.CreatedDateTime
                : note.DateTime;

            note.RoadIds ??= new List<long>();
            note.Issues ??= new List<string>();
            note.Description ??= string.Empty;
            note.RoadName ??= string.Empty;
            note.Status = "not approved";

            var created = await _notesRepo.Create(note);

            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "driver note create",
                    EntityType = "driver note",
                    EntityId = created.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) created driver note : id({created.Id})"
                });
            }

            return Ok(created);
        }

        // --------------------------------------------------
        // GET ALL / BY DRIVER ID
        // --------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? driverId)
        {
            if (!string.IsNullOrWhiteSpace(driverId))
            {
                var byDriver = await _notesRepo.GetByDriverId(driverId);
                return Ok(byDriver);
            }

            var notes = await _notesRepo.GetAll();
            return Ok(notes);
        }

        // --------------------------------------------------
        // GET BY ID
        // --------------------------------------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var note = await _notesRepo.GetById(id);
            return note == null ? NotFound() : Ok(note);
        }

        // --------------------------------------------------
        // GET NOTES WHERE ISSUES FACED = TRUE
        // --------------------------------------------------
        [HttpGet("issues-faced")]
        public async Task<IActionResult> GetIssuesFaced()
        {
            var notes = await _notesRepo.GetWithIssuesFaced();
            return Ok(notes);
        }

        // --------------------------------------------------
        // UPDATE DRIVER NOTE
        // --------------------------------------------------
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] DriverNote updatedNote)
        {
            if (updatedNote == null)
                return BadRequest();

            var updated = await _notesRepo.Update(id, updatedNote);
            if (updated == null)
                return NotFound();

            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "driver note update",
                    EntityType = "driver note",
                    EntityId = updated.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) updated driver note : id({updated.Id})"
                });
            }

            return Ok(updated);
        }

        // --------------------------------------------------
        // DELETE DRIVER NOTE
        // --------------------------------------------------
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            var deleted = await _notesRepo.Delete(id);
            if (deleted == null)
                return NotFound();

            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "driver note delete",
                    EntityType = "driver note",
                    EntityId = deleted.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) deleted driver note : id({deleted.Id})"
                });
            }

            return Ok(deleted);
        }

        // --------------------------------------------------
        // APPROVE SINGLE DRIVER NOTE
        // --------------------------------------------------
        [HttpPut("{id}/approve")]
        [Authorize]
        public async Task<IActionResult> Approve(string id)
        {
            var approvedNote = await _notesRepo.Approve(id);
            if (approvedNote == null)
                return NotFound();

            var createdRestrictions = await _restrictionsRepo.BulkCreate(
                approvedNote.RoadIds,
                approvedNote.Issues,
                approvedNote.DateTime
            );

            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "driver note approve",
                    EntityType = "driver note",
                    EntityId = approvedNote.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) approved driver note : id({approvedNote.Id})"
                });
            }

            return Ok(new
            {
                approvedNote,
                createdRoadRestrictions = createdRestrictions
            });
        }

        // --------------------------------------------------
        // BULK APPROVE DRIVER NOTES (LOG PER NOTE)
        // --------------------------------------------------
        [HttpPut("bulk-approve")]
        [Authorize]
        public async Task<IActionResult> BulkApprove([FromBody] List<string> noteIds)
        {
            if (noteIds == null || noteIds.Count == 0)
                return BadRequest();

            var approvedNotes = await _notesRepo.BulkApprove(noteIds);
            var allRestrictions = new List<RoadRestriction>();

            foreach (var note in approvedNotes)
            {
                var restrictions = await _restrictionsRepo.BulkCreate(
                    note.RoadIds,
                    note.Issues,
                    note.DateTime
                );

                allRestrictions.AddRange(restrictions);

                var userId = User.FindFirst("userId")?.Value;
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userId != null && email != null && role != null)
                {
                    await _logsRepo.CreateAsync(new ActivityLog
                    {
                        Timestamp = DateTime.UtcNow,
                        Action = "driver note approve",
                        EntityType = "driver note",
                        EntityId = note.Id,
                        UserId = userId,
                        UserName = email,
                        Message =
                            $"{email} ({role} : id({userId})) approved driver note : id({note.Id})"
                    });
                }
            }

            return Ok(new
            {
                approvedNotes,
                createdRoadRestrictions = allRestrictions
            });
        }
    }
}
