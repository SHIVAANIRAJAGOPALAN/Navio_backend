using Microsoft.AspNetCore.Mvc;
using NavioBackend.Interfaces;
using NavioBackend.Models;
using NavioBackend.DTOs;

namespace NavioBackend.Controllers
{
    [ApiController]
    [Route("api/restrictions")]
    public class RoadRestrictionsController : ControllerBase
    {
        private readonly IRoadRestrictionsRepository _repo;

        public RoadRestrictionsController(IRoadRestrictionsRepository repo)
        {
            _repo = repo;
        }

        // --------------------------------------------------
        // CREATE SINGLE ROAD RESTRICTION
        // --------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoadRestriction restriction)
        {
            if (restriction == null)
                return BadRequest();

            var created = await _repo.Create(restriction);
            return Ok(created);
        }

        // --------------------------------------------------
        // CREATE BULK ROAD RESTRICTIONS
        // --------------------------------------------------
        [HttpPost("bulk")]
        public async Task<IActionResult> BulkCreate([FromBody] BulkRestrictionRequest request)
        {
            if (request == null)
                return BadRequest();

            var created = await _repo.BulkCreate(
                request.RoadIds,
                request.Issues,
                request.DateTime
            );

            return Ok(created);
        }

        // --------------------------------------------------
        // GET ALL RESTRICTIONS
        // --------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _repo.GetAll();
            return Ok(list);
        }

        // --------------------------------------------------
        // GET RESTRICTIONS BY DATE
        // --------------------------------------------------
        [HttpGet("by-date")]
        public async Task<IActionResult> GetByDate(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var list = await _repo.GetByDate(startDate, endDate);
            return Ok(list);
        }
    }
}
