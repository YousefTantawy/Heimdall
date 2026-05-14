using HeimdallBackend.Data;
using HeimdallBackend.Models;
using HeimdallBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HeimdallBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AgentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly TokenService _tokenService;

        public AgentsController(ApplicationDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public class GenerateAgentRequest
        {
            public string AgentName { get; set; } = string.Empty;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateToken([FromBody] GenerateAgentRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized("Invalid user token.");

            var maxId = await _context.Agent
                .Where(at => at.UserId == userId)
                .MaxAsync(at => (int?)at.AgentId) ?? 0;

            var newAgentId = maxId + 1;

            var jwtString = _tokenService.CreateAgentToken(userId, newAgentId);

            var newAgent = new Agent
            {
                UserId = userId,
                AgentId = newAgentId,
                AgentName = string.IsNullOrWhiteSpace(request.AgentName) ? $"Agent-{newAgentId}" : request.AgentName,
                Token = jwtString,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Agent.Add(newAgent);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Copy this token to your Agent's docker-compose.yml. It will not be shown again.",
                AgentId = newAgent.AgentId,
                AgentName = newAgent.AgentName,
                Token = jwtString
            });
        }

        [HttpDelete("{agentId}")]
        public async Task<IActionResult> DeactivateAgent(int agentId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized("Invalid user token.");

            var agent = await _context.Agent
                .FirstOrDefaultAsync(a => a.UserId == userId && a.AgentId == agentId);

            if (agent == null) return NotFound("Agent not found.");

            if (!agent.IsActive) return Ok(new { Message = "Agent is already deactivated." });

            agent.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Agent '{agent.AgentName}' (ID: {agent.AgentId}) has been revoked. The Hub will now reject its data."
            });
        }
    }
}