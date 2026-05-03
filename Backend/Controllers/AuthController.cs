using BCrypt.Net;
using HeimdallBackend.DTOs;
using HeimdallBackend.Models;
using HeimdallBackend.Data;
using HeimdallBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeimdallBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        private readonly TokenService _tokenService;

        private readonly string[] _allowedDomains = { "gmail.com", "yahoo.com", "outlook.com", "hotmail.com" };

        public AuthController(ApplicationDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        // ------------------------------------------------------------------ Login APIs  ------------------------------------------------------------------

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto request)
        {
            // Validate email domain
            var emailDomain = request.Email.Split('@').LastOrDefault()?.ToLower();
            if (emailDomain == null || !_allowedDomains.Contains(emailDomain))
            {
                return BadRequest("Invalid email provider. Please use a supported provider like Gmail or Yahoo.");
            }

            // Check for duplicate emails
            var normalizedEmail = request.Email.ToLower().Trim();
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
            {
                return BadRequest("Email is already registered.");
            }

            // Check for duplicate usernames
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == request.Username.Trim().ToLower()))
            {
                return BadRequest("This username is already taken.");
            }

            // Hash the password using the BCrypt library
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Make the new user using the RegisterDto
            var newUser = new User
            {
                Username = request.Username.Trim(),
                Email = normalizedEmail,
                PasswordHash = passwordHash,
                RoleId = request.Roles
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var token = _tokenService.CreateToken(newUser);

            return Ok(new
            {
                message = "Registration successful!",
                userId = newUser.UserId,
                Token = token
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            var normalizedEmail = request.Email.ToLower().Trim();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid email or password.");
            }

            var token = _tokenService.CreateToken(user);

            return Ok(new
            {
                message = "Login successful!",
                userId = user.UserId,
                Token = token
            });
        }
    }
}