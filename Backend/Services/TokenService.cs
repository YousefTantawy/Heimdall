using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using HeimdallBackend.Models;

namespace HeimdallBackend.Services
{
    public class TokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public string CreateUserToken(User user)
        {
            string roleName = user.RoleId switch
            {
                1 => "User",
                2 => "Admin",
                _ => "User"
            };

            var issuer = _config["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is missing from configuration.");
            var audience = _config["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is missing from configuration.");
            var keyString = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing from configuration.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? "UnknownUser"),
                new Claim(ClaimTypes.Email, user.Email ?? "UnknownEmail"),
                new Claim(ClaimTypes.Role, roleName),
                new Claim(JwtRegisteredClaimNames.Iss, issuer),
                new Claim(JwtRegisteredClaimNames.Aud, audience)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token); 
        }

        public string CreateAgentToken(int userId, int agentId)
        {
            var issuer = _config["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer missing");
            var audience = _config["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience missing");
            var keyString = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key missing");

            var claims = new List<Claim>
            {
                new Claim("UserId", userId.ToString()),
                new Claim("AgentId", agentId.ToString()),
                new Claim(ClaimTypes.Role, "Agent"), 
                new Claim(JwtRegisteredClaimNames.Iss, issuer),
                new Claim(JwtRegisteredClaimNames.Aud, audience)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(365),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}