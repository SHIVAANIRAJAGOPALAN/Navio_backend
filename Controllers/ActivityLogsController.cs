using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NavioBackend.Interfaces;

namespace NavioBackend.Controllers
{
    [ApiController]
    [Route("api/activity-logs")]
    public class ActivityLogsController : ControllerBase
    {
        private readonly IActivityLogsRepository _repository;

        public ActivityLogsController(IActivityLogsRepository repository)
        {
            _repository = repository;
        }

        // GET ALL LOGS (admin / dashboard)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var logs = await _repository.GetAllAsync();
            return Ok(logs);
        }

        // CLEAR ALL LOGS (admin only)
        [HttpDelete("clear")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ClearAll()
        {
            await _repository.ClearAllAsync();
            return Ok(new { success = true });
        }
    }
}
