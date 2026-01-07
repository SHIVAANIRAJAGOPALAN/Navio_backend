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
    [Route("api/fleet-managers")]
    public class FleetManagersController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly ITruckRepository _truckRepo;
        private readonly IActivityLogsRepository _logsRepo;
        private readonly ITripRepository _tripRepo;

        public FleetManagersController(
            IUserRepository userRepo,
            ITruckRepository truckRepo,
            IActivityLogsRepository logsRepo,
            ITripRepository tripRepo)
        {
            _userRepo = userRepo;
            _truckRepo = truckRepo;
            _logsRepo = logsRepo;
            _tripRepo = tripRepo;
        }

        // -------------------------------------------------------------------------
        // GET /api/fleet-managers
        // -------------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _userRepo.GetByRoleAsync("FleetManager");
            var drivers = await _userRepo.GetByRoleAsync("Driver");
            var trucks = await _truckRepo.GetAllAsync();

            var shaped = list.Select(fm => new
            {
                id = fm.Id,
                name = fm.FullName,
                email = fm.Email,
                phone = fm.Phone,
                status = fm.Status,
                assignedDriverIds = drivers
        .Where(d => d.AssignedFleetManagerId == fm.Id)
        .Select(d => d.Id)
        .ToList(),

                assignedTruckIds = trucks
        .Where(t => t.AssignedFleetManagerId == fm.Id)
        .Select(t => t.Id)
        .ToList(),

                assignedDriversCount = drivers.Count(d => d.AssignedFleetManagerId == fm.Id),
                assignedTrucksCount = trucks.Count(t => t.AssignedFleetManagerId == fm.Id)
            });

            return Ok(shaped);
        }

        // -------------------------------------------------------------------------
        // GET /api/fleet-managers/{id}
        // -------------------------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest(new { message = "Invalid ID format" });

            var fm = await _userRepo.GetByIdAsync(id);
            if (fm == null || !fm.Role.Equals("FleetManager", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = "Fleet manager not found" });

            var drivers = await _userRepo.GetByRoleAsync("Driver");
            var trucks = await _truckRepo.GetAllAsync();

            var assignedDriverIds = drivers
                .Where(d => d.AssignedFleetManagerId == fm.Id)
                .Select(d => d.Id)
                .ToList();

            var assignedTruckIds = trucks
                .Where(t => t.AssignedFleetManagerId == fm.Id)
                .Select(t => t.Id)
                .ToList();


            return Ok(new
            {
                id = fm.Id,
                name = fm.FullName,
                fullName = fm.FullName,
                email = fm.Email,
                phone = fm.Phone,
                status = fm.Status,
                role = fm.Role,
                assignedDriverIds,
                assignedTruckIds,
                assignedDriversCount = assignedDriverIds.Count,
                assignedTrucksCount = assignedTruckIds.Count
            });
        }

        // GET /api/fleet-managers/{id}/assigned-assets
        [HttpGet("{id}/assigned-assets")]
        public async Task<IActionResult> GetAssignedAssets(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest(new { message = "Invalid ID format" });

            var fm = await _userRepo.GetByIdAsync(id);
            if (fm == null || !fm.Role.Equals("FleetManager", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = "Fleet manager not found" });

            var drivers = (await _userRepo.GetByRoleAsync("Driver"))
    .Where(d => d.AssignedFleetManagerId == fm.Id)
    .Select(d => new
    {
        id = d.Id,
        name = d.FullName,
        email = d.Email,
        phone = d.Phone,
        status = d.Status,
        truck = d.Truck,
        driverId = d.DriverId,
        lastLogin = d.LastLogin
    })
    .ToList();

            var trucks = (await _truckRepo.GetAllAsync())
                .Where(t => t.AssignedFleetManagerId == fm.Id)
                .Select(t => new
                {
                    id = t.Id,
                    number = t.TruckNumber,
                    Length = t.Length,
                    Width = t.Width,
                    Height = t.Height,
                    capacity = (object)t.Capacity,
                    capacityUnit = t.CapacityUnit ?? "lbs",
                    bodyType = t.BodyType,
                    dutyClass = t.DutyClass,
                    status = t.Status ?? "Unknown"
                })
                .ToList();

            return Ok(new { drivers, trucks });

        }


        // -------------------------------------------------------------------------
        // POST /api/fleet-managers
        // -------------------------------------------------------------------------
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] FleetManagerCreateDto dto)
        {
            if (dto == null) return BadRequest("Invalid payload");

            var name = dto.Name ?? dto.FullName;
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Name is required");

            var user = new User
            {
                FullName = name,
                Email = dto.Email,
                Phone = dto.Phone,
                Status = dto.Status ?? "Active",
                Role = "FleetManager",
                AssignedDriverIds = new List<string>(),
                AssignedTruckIds = new List<string>(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("f")
            };

            var created = await _userRepo.CreateAsync(user);

            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "fleet manager create",
                    EntityType = "fleet manager",
                    EntityId = created.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) created fleet manager {created.FullName} (id:{created.Id})"
                });
            }

            return Ok(new
            {
                id = created.Id,
                name = created.FullName,
                email = created.Email,
                phone = created.Phone,
                status = created.Status,
                // assignedDriverIds = created.AssignedDriverIds ?? new List<string>(),
                // assignedTruckIds = created.AssignedTruckIds ?? new List<string>(),
                assignedDriversCount = created.AssignedDriverIds?.Count ?? 0,
                assignedTrucksCount = created.AssignedTruckIds?.Count ?? 0
            });
        }

        // -------------------------------------------------------------------------
        // PUT /api/fleet-managers/{id}
        // -------------------------------------------------------------------------
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, [FromBody] FleetManagerUpdateDto dto)
        {
            if (!ObjectId.TryParse(id, out _)) return BadRequest("Invalid ID");
            if (dto == null) return BadRequest("Invalid payload");

            var existing = await _userRepo.GetByIdAsync(id);
            if (existing == null || !existing.Role.Equals("FleetManager", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            var incoming = new User
            {
                FullName = dto.Name ?? dto.FullName ?? existing.FullName,
                Email = dto.Email ?? existing.Email,
                Phone = dto.Phone ?? existing.Phone,
                Status = dto.Status ?? existing.Status,
            };

            await _userRepo.UpdateAsync(id, incoming);
            var refreshed = await _userRepo.GetByIdAsync(id);

            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "fleet manager update",
                    EntityType = "fleet manager",
                    EntityId = refreshed.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) updated fleet manager {refreshed.FullName} (id:{refreshed.Id})"
                });
            }

            return Ok(new
            {
                id = refreshed.Id,
                name = refreshed.FullName,
                email = refreshed.Email,
                phone = refreshed.Phone,
                status = refreshed.Status,
                // assignedDriverIds = refreshed.AssignedDriverIds ?? new List<string>(),
                // assignedTruckIds = refreshed.AssignedTruckIds ?? new List<string>()
            });
        }

        // -------------------------------------------------------------------------
        // PUT /api/fleet-managers/{id}/assign-drivers
        // -------------------------------------------------------------------------
        [HttpPut("{id}/assign-drivers")]
        [Authorize]
        public async Task<IActionResult> AssignDrivers(string id, [FromBody] AssignAssetsDto dto)
        {
            var fm = await _userRepo.GetByIdAsync(id);
            if (fm == null || fm.Role != "FleetManager")
                return NotFound();

            var drivers = await _userRepo.GetByRoleAsync("Driver");

            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // UNASSIGN drivers removed from this FM
            foreach (var d in drivers.Where(d =>
                d.AssignedFleetManagerId == fm.Id &&
                !dto.DriverIds.Contains(d.Id)))
            {
                await _userRepo.UpdateFleetManagerAssignmentAsync(d.Id, null);

                if (userId != null && email != null && role != null)
                {
                    await _logsRepo.CreateAsync(new ActivityLog
                    {
                        Timestamp = DateTime.UtcNow,
                        Action = "driver unassign",
                        EntityType = "driver",
                        EntityId = d.Id,
                        UserId = userId,
                        UserName = email,
                        Message =
                            $"{email} ({role} : id({userId})) unassigned driver {d.FullName} (id:{d.Id}) from fleet manager {fm.FullName} (id:{fm.Id})"
                    });
                }
            }

            // ASSIGN selected drivers
            foreach (var did in dto.DriverIds)
            {
                var d = drivers.FirstOrDefault(x => x.Id == did);

                if (d == null)
                    continue;
                if (d.AssignedFleetManagerId == fm.Id)
                    continue;
                await _userRepo.UpdateFleetManagerAssignmentAsync(did, fm.Id);

                if (userId != null && email != null && role != null)
                {
                    await _logsRepo.CreateAsync(new ActivityLog
                    {
                        Timestamp = DateTime.UtcNow,
                        Action = "driver assign",
                        EntityType = "driver",
                        EntityId = d.Id,
                        UserId = userId,
                        UserName = email,
                        Message =
                            $"{email} ({role} : id({userId})) assigned driver {d.FullName} (id:{d.Id}) to fleet manager {fm.FullName} (id:{fm.Id})"
                    });
                }
            }

            return Ok(true);

        }


        // -------------------------------------------------------------------------
        // PUT /api/fleet-managers/{id}/assign-trucks
        // -------------------------------------------------------------------------
        [HttpPut("{id}/assign-trucks")]
        [Authorize]
        public async Task<IActionResult> AssignTrucks(string id, [FromBody] AssignAssetsDto dto)
        {
            var fm = await _userRepo.GetByIdAsync(id);
            if (fm == null || fm.Role != "FleetManager")
                return NotFound();

            var allTrucks = await _truckRepo.GetAllAsync();

            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // UNASSIGN removed trucks
            foreach (var t in allTrucks.Where(t =>
                t.AssignedFleetManagerId == fm.Id &&
                !dto.TruckIds.Contains(t.Id)))
            {
                await _truckRepo.AssignToFleetManagerAsync(t.Id, null);

                if (userId != null && email != null && role != null)
                {
                    await _logsRepo.CreateAsync(new ActivityLog
                    {
                        Timestamp = DateTime.UtcNow,
                        Action = "truck unassign",
                        EntityType = "fleet",
                        EntityId = t.Id,
                        UserId = userId,
                        UserName = email,
                        Message =
                            $"{email} ({role} : id({userId})) unassigned truck {t.TruckNumber} (id:{t.Id}) from fleet manager {fm.FullName} (id:{fm.Id})"
                    });
                }
            }

            // ASSIGN selected trucks
            foreach (var tid in dto.TruckIds)
            {
                var t = allTrucks.FirstOrDefault(x => x.Id == tid);
                if (t == null)
                    continue;

                // 🚫 already assigned → skip
                if (t.AssignedFleetManagerId == fm.Id)
                    continue;

                await _truckRepo.AssignToFleetManagerAsync(tid, fm.Id);

                if (userId != null && email != null && role != null)
                {
                    await _logsRepo.CreateAsync(new ActivityLog
                    {
                        Timestamp = DateTime.UtcNow,
                        Action = "truck assign",
                        EntityType = "fleet",
                        EntityId = t.Id,
                        UserId = userId,
                        UserName = email,
                        Message =
                            $"{email} ({role} : id({userId})) assigned truck {t.TruckNumber} (id:{t.Id}) to fleet manager {fm.FullName} (id:{fm.Id})"
                    });
                }
            }

            return Ok(true);

        }


        // -------------------------------------------------------------------------
        // POST /api/fleet-managers/transfer
        // -------------------------------------------------------------------------
        [HttpPost("transfer")]
        [Authorize]
        public async Task<IActionResult> TransferAssets([FromBody] TransferAssetsDto dto)
        {
            var source = await _userRepo.GetByIdAsync(dto.SourceManagerId);
            var target = await _userRepo.GetByIdAsync(dto.TargetManagerId);

            if (source == null || target == null)
                return NotFound();

            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // TRANSFER DRIVERS
            foreach (var did in dto.Drivers ?? new List<string>())
            {
                await _userRepo.UpdateFleetManagerAssignmentAsync(did, target.Id);
                var d = await _userRepo.GetByIdAsync(did);

                if (d != null && userId != null && email != null && role != null)
                {
                    await _logsRepo.CreateAsync(new ActivityLog
                    {
                        Timestamp = DateTime.UtcNow,
                        Action = "driver transfer",
                        EntityType = "driver",
                        EntityId = d.Id,
                        UserId = userId,
                        UserName = email,
                        Message =
                            $"{email} ({role} : id({userId})) transferred driver {d.FullName} (id:{d.Id}) from fleet manager {source.FullName} (id:{source.Id}) to {target.FullName} (id:{target.Id})"
                    });
                }
            }


            // TRANSFER TRUCKS
            foreach (var tid in dto.Trucks ?? new List<string>())
            {
                await _truckRepo.AssignToFleetManagerAsync(tid, target.Id);
                var t = await _truckRepo.GetByIdAsync(tid);

                if (t != null && userId != null && email != null && role != null)
                {
                    await _logsRepo.CreateAsync(new ActivityLog
                    {
                        Timestamp = DateTime.UtcNow,
                        Action = "truck transfer",
                        EntityType = "fleet",
                        EntityId = t.Id,
                        UserId = userId,
                        UserName = email,
                        Message =
                            $"{email} ({role} : id({userId})) transferred truck {t.TruckNumber} (id:{t.Id}) from fleet manager {source.FullName} (id:{source.Id}) to {target.FullName} (id:{target.Id})"
                    });
                }
            }

            return Ok(true);

        }


        // -------------------------------------------------------------------------
        // DELETE /api/fleet-managers/{id}
        // Policy:
        // - Unassign drivers
        // - Unassign trucks
        // - Cancel ONLY upcoming trips
        // - Ignore other trip statuses
        // -------------------------------------------------------------------------
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest("Invalid ID");

            var fm = await _userRepo.GetByIdAsync(id);
            if (fm == null || !fm.Role.Equals("FleetManager", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            var actorUserId = User.FindFirst("userId")?.Value;
            var actorEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var actorRole = User.FindFirst(ClaimTypes.Role)?.Value;


            // ------------------------------------------------------------
            // 1. Unassign drivers
            // ------------------------------------------------------------
            var drivers = await _userRepo.GetByRoleAsync("Driver");
            var affectedDrivers = drivers
                .Where(d => d.AssignedFleetManagerId == fm.Id)
                .ToList();

            foreach (var d in affectedDrivers)
            {
                await _userRepo.UpdateFleetManagerAssignmentAsync(d.Id, null);
                if (actorUserId != null && actorEmail != null && actorRole != null)
                {
                    await _logsRepo.CreateAsync(new ActivityLog
                    {
                        Timestamp = DateTime.UtcNow,
                        Action = "driver unassign",
                        EntityType = "driver",
                        EntityId = d.Id,
                        UserId = actorUserId,
                        UserName = actorEmail,
                        Message =
                           $"Driver {d.FullName} (id:{d.Id}) unassigned from fleet manager {fm.FullName} (id:{fm.Id})"
                    });
                }
            }

            // ------------------------------------------------------------
            // 2. Unassign trucks
            // ------------------------------------------------------------
            var trucks = await _truckRepo.GetAllAsync();
            var affectedTrucks = trucks
                .Where(t => t.AssignedFleetManagerId == fm.Id)
                .ToList();

            foreach (var t in affectedTrucks)
            {
                await _truckRepo.AssignToFleetManagerAsync(t.Id, null);
                if (actorUserId != null && actorEmail != null && actorRole != null)
                {
                    await _logsRepo.CreateAsync(new ActivityLog
                    {
                        Timestamp = DateTime.UtcNow,
                        Action = "truck unassign",
                        EntityType = "truck",
                        EntityId = t.Id,
                        UserId = actorUserId,
                        UserName = actorEmail,
                        Message =
                            $"Truck {t.TruckNumber} (id:{t.Id}) unassigned from fleet manager {fm.FullName} (id:{fm.Id})"
                    });
                }
            }

            // ------------------------------------------------------------
            // 3. Cancel UPCOMING trips only
            // ------------------------------------------------------------
            var trips = await _tripRepo.GetByFleetManager(fm.Id);

            var upcomingTrips = trips
                .Where(t => string.Equals(t.Status, "Upcoming", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var trip in upcomingTrips)
            {
                trip.Status = "Cancelled";
                trip.CancellationReason = "Fleet manager deleted";

                await _tripRepo.Update(trip.Id, trip);

                if (actorUserId != null && actorEmail != null && actorRole != null)
                {
                    await _logsRepo.CreateAsync(new ActivityLog
                    {
                        Timestamp = DateTime.UtcNow,
                        Action = "trip cancel",
                        EntityType = "trip",
                        EntityId = trip.Id,
                        UserId = actorUserId,
                        UserName = actorEmail,
                        Message =
                            $"Trip (id:{trip.Id}) cancelled because fleet manager {fm.FullName} (id:{fm.Id}) was deleted"
                    });
                }
            }

            // ------------------------------------------------------------
            // 4. Delete fleet manager
            // ------------------------------------------------------------
            await _userRepo.DeleteAsync(fm.Id);

            // ------------------------------------------------------------
            // 5. Activity log
            // ------------------------------------------------------------

            if (actorUserId != null && actorEmail != null && actorRole != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "fleet manager delete",
                    EntityType = "fleet manager",
                    EntityId = fm.Id,
                    UserId = actorUserId,
                    UserName = actorEmail,
                    Message =
                        $"{actorEmail} ({actorRole} : id({actorUserId})) deleted fleet manager {fm.FullName} (id:{fm.Id}); " +
                        $"drivers unassigned: {affectedDrivers.Count}, " +
                        $"trucks unassigned: {affectedTrucks.Count}, " +
                        $"upcoming trips cancelled: {upcomingTrips.Count}"
                });
            }

            // ------------------------------------------------------------
            // 6. Response
            // ------------------------------------------------------------
            return Ok(new
            {
                message = "Fleet manager deleted successfully",
                driversUnassigned = affectedDrivers.Count,
                trucksUnassigned = affectedTrucks.Count,
                tripsCancelled = upcomingTrips.Count
            });
        }

    }
}
