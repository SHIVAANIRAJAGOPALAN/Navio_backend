using Microsoft.AspNetCore.Mvc;
using NavioBackend.Interfaces;
using NavioBackend.Models;
using NavioBackend.DTOs;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;


namespace NavioBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripsController : ControllerBase
    {
        private readonly ITripRepository _repo;
        private readonly IUserRepository _userRepo;
        private readonly IActivityLogsRepository _logsRepo;


        // NOTE: removed ITruckRepository because controller now relies on trip.Truck* fields
        public TripsController(
            ITripRepository repo,
            IUserRepository userRepo,
            IActivityLogsRepository logsRepo)
        {
            _repo = repo;
            _userRepo = userRepo;
            _logsRepo = logsRepo;
        }


        // -------------------------------------------------------
        // GET ALL / FILTER BY FLEET MANAGER / DRIVER
        // -------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? fleetManagerId, [FromQuery] string? driverId)
        {
            if (!string.IsNullOrEmpty(fleetManagerId))
                return Ok(await _repo.GetByFleetManager(fleetManagerId));

            if (!string.IsNullOrEmpty(driverId))
                return Ok(await _repo.GetByDriver(driverId));

            return Ok(await _repo.GetAll());
        }

        [HttpGet("unassigned")]
        public async Task<IActionResult> GetUnassigned([FromQuery] string fleetManagerId)
        {
            return Ok(await _repo.GetUnassigned(fleetManagerId));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var trip = await _repo.GetById(id);
            if (trip == null) return NotFound();
            return Ok(trip);
        }

        // -------------------------------
        // GET upcoming trips for driver
        // GET /api/trips/upcoming?driverId={driverId}
        // -------------------------------
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcoming([FromQuery] string driverId)
        {
            if (string.IsNullOrWhiteSpace(driverId))
                return BadRequest(new { message = "Missing driverId" });

            var all = await _repo.GetAll();
            var upcoming = all
                .Where(t => string.Equals(t.DriverId, driverId, StringComparison.OrdinalIgnoreCase)
                            && string.Equals((t.Status ?? "").Trim(), "upcoming", StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.PickupDateTime)
                .ToList();

            return Ok(upcoming);
        }

        // -------------------------------
        // GET in-progress (single) trip for driver
        // GET /api/trips/in-progress?driverId={driverId}
        // -------------------------------
        [HttpGet("in-progress")]
        public async Task<IActionResult> GetInProgress([FromQuery] string driverId)
        {
            if (string.IsNullOrWhiteSpace(driverId))
                return BadRequest(new { message = "Missing driverId" });

            var all = await _repo.GetAll();

            var inProgress = all
                .Where(t => string.Equals(t.DriverId, driverId, StringComparison.OrdinalIgnoreCase)
                            && (string.Equals((t.Status ?? "").Trim(), "ongoing", StringComparison.OrdinalIgnoreCase)
                                || string.Equals((t.Status ?? "").Trim(), "in progress", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(t => t.PickupDateTime)
                .FirstOrDefault();

            return Ok(inProgress); // null -> frontend expects null -> handled
        }

        // -------------------------------
        // GET recent trips for driver
        // GET /api/trips/recent?driverId={driverId}&limit=3
        // -------------------------------
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent([FromQuery] string driverId, [FromQuery] int limit = 3)
        {
            if (string.IsNullOrWhiteSpace(driverId))
                return BadRequest(new { message = "Missing driverId" });

            var all = await _repo.GetAll();
            var recent = all
                .Where(t => string.Equals(t.DriverId, driverId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.PickupDateTime)
                .Take(limit)
                .ToList();

            return Ok(recent);
        }

        // -------------------------------------------------------
        // CREATE TRIP (strict validation, uses trip.Truck* fields)
        // -------------------------------------------------------
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] Trip trip)
        {
            // Required top-level trip fields
            if (trip == null)
                return BadRequest(new { message = "Missing trip body." });

            if (string.IsNullOrWhiteSpace(trip.PickupLocation) ||
                string.IsNullOrWhiteSpace(trip.DropLocation) ||
                string.IsNullOrWhiteSpace(trip.DriverId) ||
                string.IsNullOrWhiteSpace(trip.TruckId))
            {
                return BadRequest(new { message = "Missing required trip fields (pickupLocation, dropLocation, driverId, truckId)." });
            }

            // Validate driver exists
            var driver = await _userRepo.GetByIdAsync(trip.DriverId);
            if (driver == null)
                return BadRequest(new { message = "Selected driver does not exist." });

            // --- Strict truck-field validation (now read from trip.Truck*)
            // Ensure required truck fields are present and numeric fields are non-negative
            if (trip.TruckCapacity <= 0)
                return BadRequest(new { message = "TruckCapacity must be > 0 (provided in trip.TruckCapacity)." });
            if (trip.TruckLength <= 0 || trip.TruckWidth <= 0 || trip.TruckHeight <= 0)
                return BadRequest(new { message = "TruckLength/TruckWidth/TruckHeight must be > 0 (provided in trip.TruckLength/TruckWidth/TruckHeight)." });

            // cargo checks (similar to frontend)
            if (trip.CargoWeight <= 0)
                return BadRequest(new { message = "CargoWeight must be > 0." });

            // Weight
            if (trip.CargoWeight > trip.TruckCapacity)
                return BadRequest(new { message = $"Cargo weight {trip.CargoWeight} exceeds truck capacity {trip.TruckCapacity}" });

            // Length / Width / Height (nullable)
            if (trip.CargoLength > trip.TruckLength)
                return BadRequest(new { message = $"Cargo length {trip.CargoLength} > truck length {trip.TruckLength}" });
            if (trip.CargoWidth > trip.TruckWidth)
                return BadRequest(new { message = $"Cargo width {trip.CargoWidth} > truck width {trip.TruckWidth}" });
            if (trip.CargoHeight > trip.TruckHeight)
                return BadRequest(new { message = $"Cargo height {trip.CargoHeight} > truck height {trip.TruckHeight}" });

            // Body Type & Duty Class checks (treat "any" as wildcard)
                if (string.IsNullOrWhiteSpace(trip.TruckBodyType))
                {
                    return BadRequest(new { message = $"TruckBodyType '{trip.TruckBodyType}' is null or empty" });
                }

                if (string.IsNullOrWhiteSpace(trip.TruckDutyClass))
                {
                    return BadRequest(new { message = $"TruckDutyClass '{trip.TruckDutyClass}' is null or empty " });
                }

            // Time conflict check (driver/truck scheduling)
            var allTrips = await _repo.GetAll();
            foreach (var t in allTrips)
            {
                // ✅ ONLY active trips should block scheduling
                if (!string.Equals(t.Status, "Upcoming", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(t.Status, "In Progress", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(t.Status, "Ongoing", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (t.DriverId == trip.DriverId || t.TruckId == trip.TruckId)
                {
                    bool overlap =
                        trip.PickupDateTime < t.ExpectedCompletionTime &&
                        trip.ExpectedCompletionTime > t.PickupDateTime;

                    if (overlap)
                        return BadRequest(new { message = "Scheduling conflict: Driver or Truck already assigned for the requested time window." });
                }
            }

            // Ensure defaults required by model/repository
            trip.SpecialInstructions ??= string.Empty;
            trip.RoadRestrictions ??= new List<string>();

            // Ensure progress defaults: non-negative values
            if (trip.DistanceTravelled < 0) trip.DistanceTravelled = 0.0;
            if (trip.Duration < 0) trip.Duration = 0.0;

            trip.Status = "Upcoming";
            await _repo.Create(trip);

            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "trip create",
                    EntityType = "trip",
                    EntityId = trip.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) created trip : {trip.PickupLocation} - {trip.DropLocation} (id:{trip.Id})"
                });
            }

            return Ok(trip);
        }

        // -------------------------------------------------------
        // UPDATE TRIP (merge editable fields; use road_restrictions)
        // -------------------------------------------------------
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] Trip updated)
        {
            if (updated == null) return BadRequest(new { message = "Missing update body." });

            var existing = await _repo.GetById(id);
            if (existing == null)
                return NotFound(new { message = "Trip not found" });

            // ---- Merge editable fields ----
            existing.PickupLocation = updated.PickupLocation ?? existing.PickupLocation;
            existing.DropLocation = updated.DropLocation ?? existing.DropLocation;

            existing.PickupDateTime = updated.PickupDateTime != default
                ? updated.PickupDateTime
                : existing.PickupDateTime;

            existing.ExpectedCompletionTime = updated.ExpectedCompletionTime != default
                ? updated.ExpectedCompletionTime
                : existing.ExpectedCompletionTime;

            existing.CargoType = updated.CargoType ?? existing.CargoType;
            existing.CargoWeight = updated.CargoWeight != 0 ? updated.CargoWeight : existing.CargoWeight;
            if (updated.CargoLength > 0) existing.CargoLength = updated.CargoLength;
if (updated.CargoWidth  > 0) existing.CargoWidth  = updated.CargoWidth;
if (updated.CargoHeight > 0) existing.CargoHeight = updated.CargoHeight;

            existing.SpecialInstructions = updated.SpecialInstructions ?? existing.SpecialInstructions;
            // replaced restrictionNotes with road_restrictions
            existing.RoadRestrictions = updated.RoadRestrictions ?? existing.RoadRestrictions;

            // Truck details can be updated (only if provided) — keep strict numeric checks if updating
            if (updated.TruckCapacity > 0) existing.TruckCapacity = updated.TruckCapacity;
            if (updated.TruckLength > 0) existing.TruckLength = updated.TruckLength;
            if (updated.TruckWidth > 0) existing.TruckWidth = updated.TruckWidth;
            if (updated.TruckHeight > 0) existing.TruckHeight = updated.TruckHeight;
            if (!string.IsNullOrWhiteSpace(updated.TruckBodyType)) existing.TruckBodyType = updated.TruckBodyType;
            if (!string.IsNullOrWhiteSpace(updated.TruckDutyClass)) existing.TruckDutyClass = updated.TruckDutyClass;

            // Status (only editable during update)
            if (!string.IsNullOrWhiteSpace(updated.Status))
                existing.Status = updated.Status;

            // Save back to DB
            await _repo.Update(id, existing);

            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "trip update",
                    EntityType = "trip",
                    EntityId = id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) updated trip : {existing.PickupLocation} - {existing.DropLocation} (id:{id})"
                });
            }


            return Ok(existing);
        }

        // -------------------------------
        //  UPDATE STATUS
        // -------------------------------
        [HttpPut("{id}/status")]
        [Authorize]
public async Task<IActionResult> UpdateStatus(string id, [FromBody] StatusUpdateDto dto)
{
    if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
        return BadRequest(new { message = "Missing or invalid 'status'." });

    string status = dto.Status.Trim();
    string? reason = null;

    // Extract "Reason" if present in DTO
    if (!string.IsNullOrWhiteSpace(dto.Reason))
        reason = dto.Reason!.Trim();

    try
    {
        // 1) Update basic status
        await _repo.UpdateStatus(id, status);

        // 2) If CANCELLED and reason exists → store cancellation reason
        if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(reason))
        {
            await _repo.UpdateCancellationReason(id, reason);
        }

        // 3) Fetch updated trip and return it
        var updated = await _repo.GetById(id);
        if (updated == null)
            return NotFound(new { message = "Trip not found after status update." });

        var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                string action;
                string verb;

                if (status.Equals("In Progress", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("Ongoing", StringComparison.OrdinalIgnoreCase))
                {
                    action = "trip start";
                    verb = "started";
                }
                else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    action = "trip complete";
                    verb = "completed";
                }
                else if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    action = "trip cancel";
                    verb = "cancelled";
                }
                else
                {
                    action = "trip update";
                    verb = "updated";
                }

                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = action,
                    EntityType = "trip",
                    EntityId = updated.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) {verb} trip : {updated.PickupLocation} - {updated.DropLocation} (id:{updated.Id})"
                });
            }

        return Ok(new
        {
            message = "Status updated",
            trip = updated
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { 
            message = "Error updating status", 
            detail = ex.Message 
        });
    }
}


        // -------------------------------
        //  UPDATE PROGRESS (DTO based)
        //  Accepts: DistanceTravelled, Duration
        // -------------------------------
        [HttpPut("{id}/progress")]
public async Task<IActionResult> UpdateProgress(string id, [FromBody] ProgressUpdateDto dto)
{
    if (dto == null)
        return BadRequest(new { message = "Invalid payload" });

    var existingTrip = await _repo.GetById(id);
    if (existingTrip == null)
        return NotFound(new { message = "Trip not found" });

    var progress = new Trip
    {
        DistanceTravelled = dto.DistanceTravelled,
        Duration = dto.Duration
        // Status intentionally excluded
    };

    try
    {
        await _repo.UpdateProgress(id, progress);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Error updating progress", detail = ex.Message });
    }

    return Ok();
}


        // -------------------------------------------------------
        // DELETE TRIP
        // -------------------------------------------------------
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _repo.GetById(id);

            await _repo.Delete(id);
            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "trip delete",
                    EntityType = "trip",
                    EntityId = id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) deleted trip : {existing.PickupLocation} - {existing.DropLocation} (id:{id})"
                });
            }

            return Ok();
        }
    }
}
