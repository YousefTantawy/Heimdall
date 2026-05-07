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

        public string CreateToken(User user)
        {
            string roleName = user.RoleId switch
            {
                1 => "User",
                2 => "Admin",
                _ => "User"
            };

            // 1. Safely extract configuration with hardcoded fallbacks if Docker fails
            var issuer = _config["JwtSettings:Issuer"] ?? "http://localhost:5046";
            var audience = _config["JwtSettings:Audience"] ?? "http://localhost:5046";

            // 2. Build the claims list with null-coalescing operators to prevent crashes
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? "UnknownUser"),
                new Claim(ClaimTypes.Email, user.Email ?? "UnknownEmail"),
                new Claim(ClaimTypes.Role, roleName),
                new Claim(JwtRegisteredClaimNames.Iss, issuer),
                new Claim(JwtRegisteredClaimNames.Aud, audience)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            // 3. The Token (Putting it all together)
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
    }
}