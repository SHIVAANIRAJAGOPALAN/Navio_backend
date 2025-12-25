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

        public FleetManagersController(
            IUserRepository userRepo,
            ITruckRepository truckRepo,
            IActivityLogsRepository logsRepo)
        {
            _userRepo = userRepo;
            _truckRepo = truckRepo;
            _logsRepo = logsRepo;
        }

        // -------------------------------------------------------------------------
        // GET /api/fleet-managers
        // -------------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _userRepo.GetByRoleAsync("FleetManager");

            var shaped = list.Select(fm => new
            {
                id = fm.Id,
                name = fm.FullName,
                email = fm.Email,
                phone = fm.Phone,
                status = fm.Status,
                assignedDriverIds = fm.AssignedDriverIds ?? new List<string>(),
                assignedTruckIds = fm.AssignedTruckIds ?? new List<string>(),
                assignedDriversCount = fm.AssignedDriverIds?.Count ?? 0,
                assignedTrucksCount = fm.AssignedTruckIds?.Count ?? 0
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

            return Ok(new
            {
                id = fm.Id,
                name = fm.FullName,
                fullName = fm.FullName,
                email = fm.Email,
                phone = fm.Phone,
                status = fm.Status,
                role = fm.Role,
                assignedDriverIds = fm.AssignedDriverIds ?? new List<string>(),
                assignedTruckIds = fm.AssignedTruckIds ?? new List<string>(),
                assignedDriversCount = fm.AssignedDriverIds?.Count ?? 0,
                assignedTrucksCount = fm.AssignedTruckIds?.Count ?? 0
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

            var driverIds = fm.AssignedDriverIds ?? new List<string>();
            var truckIds = fm.AssignedTruckIds ?? new List<string>();

            // --- drivers (keep as before, include LastLogin if present) ---
            var drivers = new List<object>();
            foreach (var did in driverIds)
            {
                var d = await _userRepo.GetByIdAsync(did);
                if (d == null) continue;

                drivers.Add(new
                {
                    id = d.Id,
                    name = d.FullName,
                    email = d.Email,
                    phone = d.Phone,
                    status = d.Status,
                    truck = d.Truck,
                    driverId = d.DriverId,
                    // LastLogin is DateTime on your User model; include it directly (frontend can format)
                    lastLogin = d.LastLogin
                });
            }

            // --- trucks: return exact fields frontend expects ---
            var trucks = new List<object>();
            foreach (var tid in truckIds)
            {
                var t = await _truckRepo.GetByIdAsync(tid);
                if (t == null) continue;

                // Note: Truck model (Truck.cs) has properties:
                //   public string TruckNumber { get; set; }
                //   public double Length { get; set; }
                //   public double Width { get; set; }
                //   public double Height { get; set; }
                //   [BsonElement("CapacityLbs")] public int Capacity { get; set; }
                //   public string CapacityUnit { get; set; } = "lbs";
                //   public string? BodyType { get; set; }
                //   public string? DutyClass { get; set; }
                //
                // We map those to the frontend shape (Length/Width/Height camel-preserved as requested).

                trucks.Add(new
                {
                    id = t.Id,
                    number = t.TruckNumber ?? t.TruckNumber, // map TruckNumber -> number
                    // dimensions (non-nullable on model so return values directly)
                    Length = t.Length,
                    Width = t.Width,
                    Height = t.Height,

                    // capacity (Capacity property maps to CapacityLbs in DB)
                    capacity = (object)t.Capacity,     // boxed to allow JSON nullability if needed by serializer
                    capacityUnit = t.CapacityUnit ?? "lbs",

                    bodyType = t.BodyType,
                    dutyClass = t.DutyClass,

                    status = t.Status ?? "Unknown"
                });
            }

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
                AssignedDriverIds = dto.AssignedDriverIds ?? new List<string>(),
                AssignedTruckIds = dto.AssignedTruckIds ?? new List<string>(),
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
                assignedDriverIds = created.AssignedDriverIds ?? new List<string>(),
                assignedTruckIds = created.AssignedTruckIds ?? new List<string>(),
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
                AssignedDriverIds = dto.AssignedDriverIds ?? existing.AssignedDriverIds,
                AssignedTruckIds = dto.AssignedTruckIds ?? existing.AssignedTruckIds
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
                assignedDriverIds = refreshed.AssignedDriverIds ?? new List<string>(),
                assignedTruckIds = refreshed.AssignedTruckIds ?? new List<string>()
            });
        }

        // -------------------------------------------------------------------------
        // PUT /api/fleet-managers/{id}/assign-drivers
        // -------------------------------------------------------------------------
        [HttpPut("{id}/assign-drivers")]
        [Authorize]
        public async Task<IActionResult> AssignDrivers(string id, [FromBody] AssignAssetsDto dto)
        {
            if (!ObjectId.TryParse(id, out _)) return BadRequest("Invalid ID");
            if (dto?.DriverIds == null) return BadRequest("driverIds required");

            var fm = await _userRepo.GetByIdAsync(id);
            if (fm == null || !fm.Role.Equals("FleetManager", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            var newIds = dto.DriverIds.Distinct().ToList();
            var oldIds = fm.AssignedDriverIds ?? new List<string>();

            var toAdd = newIds.Except(oldIds).ToList();
            var toRemove = oldIds.Except(newIds).ToList();

            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // ---------- UNASSIGN REMOVED DRIVERS ----------
            foreach (var did in toRemove)
            {
                var d = await _userRepo.GetByIdAsync(did);
                if (d == null || d.Role != "Driver") continue;

                if (d.AssignedFleetManagerId == fm.Id)
                {
                    await _userRepo.ClearAssignedFleetManagerAsync(did);


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
            }

            // ---------- ASSIGN NEW DRIVERS ----------
            foreach (var did in toAdd)
            {
                var d = await _userRepo.GetByIdAsync(did);
                if (d == null || d.Role != "Driver") continue;

                // remove from previous FM if needed
                if (!string.IsNullOrEmpty(d.AssignedFleetManagerId) &&
                    d.AssignedFleetManagerId != fm.Id)
                {
                    var prevFm = await _userRepo.GetByIdAsync(d.AssignedFleetManagerId);
                    if (prevFm != null)
                    {
                        var cleaned = (prevFm.AssignedDriverIds ?? new List<string>())
                                    .Where(x => x != d.Id).ToList();
                        await _userRepo.UpdateAsync(prevFm.Id,
                            new User { AssignedDriverIds = cleaned });
                    }
                }

                await _userRepo.UpdateAsync(did,
                    new User { AssignedFleetManagerId = fm.Id });

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

            // ---------- REPLACE FM LIST ----------
            await _userRepo.UpdateAsync(fm.Id,
                new User { AssignedDriverIds = newIds });

            return Ok(true);
        }


        // -------------------------------------------------------------------------
        // PUT /api/fleet-managers/{id}/assign-trucks
        // -------------------------------------------------------------------------
        [HttpPut("{id}/assign-trucks")]
        [Authorize]
        public async Task<IActionResult> AssignTrucks(string id, [FromBody] AssignAssetsDto dto)
        {
            if (!ObjectId.TryParse(id, out _)) return BadRequest("Invalid ID");
            if (dto?.TruckIds == null) return BadRequest("truckIds required");

            var fm = await _userRepo.GetByIdAsync(id);
            if (fm == null || !fm.Role.Equals("FleetManager", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            var newIds = dto.TruckIds.Distinct().ToList();
            var oldIds = fm.AssignedTruckIds ?? new List<string>();

            var toAdd = newIds.Except(oldIds).ToList();
            var toRemove = oldIds.Except(newIds).ToList();

            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // ---------- UNASSIGN REMOVED TRUCKS ----------
            foreach (var tid in toRemove)
            {
                var t = await _truckRepo.GetByIdAsync(tid);
                if (t == null) continue;

                if (t.AssignedFleetManagerId == fm.Id)
                {
                    await _truckRepo.AssignToFleetManagerAsync(tid, null);

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
            }

            // ---------- ASSIGN NEW TRUCKS ----------
            foreach (var tid in toAdd)
            {
                var t = await _truckRepo.GetByIdAsync(tid);
                if (t == null) continue;

                // cleanup previous FM
                if (!string.IsNullOrEmpty(t.AssignedFleetManagerId) &&
                    t.AssignedFleetManagerId != fm.Id)
                {
                    var prevFm = await _userRepo.GetByIdAsync(t.AssignedFleetManagerId);
                    if (prevFm != null)
                    {
                        var cleaned = (prevFm.AssignedTruckIds ?? new List<string>())
                                    .Where(x => x != t.Id).ToList();
                        await _userRepo.UpdateAsync(prevFm.Id,
                            new User { AssignedTruckIds = cleaned });
                    }
                }

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

            // ---------- REPLACE FM LIST ----------
            await _userRepo.UpdateAsync(fm.Id,
                new User { AssignedTruckIds = newIds });

            return Ok(true);
        }


        // -------------------------------------------------------------------------
        // POST /api/fleet-managers/transfer
        // -------------------------------------------------------------------------
        [HttpPost("transfer")]
        [Authorize]
        public async Task<IActionResult> TransferAssets([FromBody] TransferAssetsDto dto)
        {
            if (dto == null) return BadRequest("Invalid payload");

            var source = await _userRepo.GetByIdAsync(dto.SourceManagerId);
            var target = await _userRepo.GetByIdAsync(dto.TargetManagerId);
            if (source == null || target == null) return NotFound();

            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // ---------- TRANSFER DRIVERS ----------
            foreach (var did in dto.Drivers ?? new List<string>())
            {
                var d = await _userRepo.GetByIdAsync(did);
                if (d == null || d.Role != "Driver") continue;

                // remove from source FM
                var srcDrivers = (source.AssignedDriverIds ?? new List<string>())
                                .Where(x => x != d.Id).ToList();
                await _userRepo.UpdateAsync(source.Id,
                    new User { AssignedDriverIds = srcDrivers });

                // add to target FM
                var tgtDrivers = (target.AssignedDriverIds ?? new List<string>());
                if (!tgtDrivers.Contains(d.Id)) tgtDrivers.Add(d.Id);
                await _userRepo.UpdateAsync(target.Id,
                    new User { AssignedDriverIds = tgtDrivers });

                await _userRepo.UpdateAsync(d.Id,
                    new User { AssignedFleetManagerId = target.Id });

                if (userId != null && email != null && role != null)
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

            // ---------- TRANSFER TRUCKS ----------
            foreach (var tid in dto.Trucks ?? new List<string>())
            {
                var t = await _truckRepo.GetByIdAsync(tid);
                if (t == null) continue;

                var srcTrucks = (source.AssignedTruckIds ?? new List<string>())
                                .Where(x => x != t.Id).ToList();
                await _userRepo.UpdateAsync(source.Id,
                    new User { AssignedTruckIds = srcTrucks });

                var tgtTrucks = (target.AssignedTruckIds ?? new List<string>());
                if (!tgtTrucks.Contains(t.Id)) tgtTrucks.Add(t.Id);
                await _userRepo.UpdateAsync(target.Id,
                    new User { AssignedTruckIds = tgtTrucks });

                await _truckRepo.AssignToFleetManagerAsync(tid, target.Id);

                if (userId != null && email != null && role != null)
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
        // -------------------------------------------------------------------------
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            if (!ObjectId.TryParse(id, out _)) return BadRequest("Invalid ID");

            var existing = await _userRepo.GetByIdAsync(id);
            if (existing == null || !existing.Role.Equals("FleetManager", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            await _userRepo.DeleteAsync(id);

            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId != null && email != null && role != null)
            {
                await _logsRepo.CreateAsync(new ActivityLog
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "fleet manager delete",
                    EntityType = "fleet manager",
                    EntityId = existing.Id,
                    UserId = userId,
                    UserName = email,
                    Message =
                        $"{email} ({role} : id({userId})) deleted fleet manager {existing.FullName} (id:{existing.Id})"
                });
            }

            return Ok(new { message = "Fleet manager deleted successfully" });
        }
    }
}
