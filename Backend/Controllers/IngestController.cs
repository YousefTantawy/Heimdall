using HeimdallBackend.DTOs;
using HeimdallBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeimdallBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class IngestController : ControllerBase
    {
        private readonly ILogger<IngestController> _logger;
        public IngestController(ILogger<IngestController> logger)
        {
            _logger = logger;
        }

        //[HttpPost]
        //public async Task<IActionResult> ReceiveFlows([FromBody] AgentPayloadDtos payload)
        //{
        //    if (payload.Flows == null || !payload.Flows.Any())
        //    {
        //        return BadRequest("Payload contains no network flows.");
        //    }

        //    _logger.LogInformation("Received {Count} flows from Agent: {AgentId}",
        //        payload.Flows.Count, payload.AgentId);

        //    return Ok(new { Message = "Payload accepted." });
        //}

        [HttpPost]
        public IActionResult ReceiveFlows([FromBody] AgentPayloadDtos payload)
        {
            if (payload.Flows == null || payload.Flows.Count == 0)
            {
                return BadRequest(new { Message = "Payload contains no network flows." });
            }

            _logger.LogInformation("Received {Count} flows from Agent: {AgentId}",
                payload.Flows.Count, payload.AgentId);

            _logger.LogInformation("--- START OF BATCH FROM {AgentId} ---", payload.AgentId);
            foreach (var row in payload.Flows)
            {
                _logger.LogInformation("{Row}", row);
            }
            _logger.LogInformation("--- END OF BATCH ---");

            return Ok(new { Message = "Payload accepted." });
        }
    }
}
