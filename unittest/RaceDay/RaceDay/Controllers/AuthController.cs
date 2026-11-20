using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Database;
using RaceDay.Models;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDBContext _dbContext;
        private readonly PasswordHasher<Organiser> _organiserHasher = new();
        private readonly PasswordHasher<Participant> _participantHasher = new();

        public AuthController(RaceDayDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ==========================================
        // 1. REGISTRATION
        // ==========================================

        [HttpPost("participants/register")]
        public async Task<IActionResult> ParticipantRegister([FromBody] ParticipantRegisterDto dto)
        {
            if (await _dbContext.Participant.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
            {
                return Conflict(new { message = "Phone number already registered" });
            }

            var newUser = new Participant
            {
                Name = dto.Name,
                Age = dto.Age,
                AvatarUrl = dto.AvatarUrl ?? string.Empty,
                PhoneNumber = dto.PhoneNumber
            };

            newUser.Password = _participantHasher.HashPassword(newUser, dto.Password);

            _dbContext.Participant.Add(newUser);
            await _dbContext.SaveChangesAsync();

            return StatusCode(201, new { message = "Participant registered successfully" });
        }

        [HttpPost("organisers/register")]
        public async Task<IActionResult> OrganiserRegister([FromBody] OrganiserRegisterDto dto)
        {
            if (await _dbContext.Organiser.AnyAsync(o => o.Name == dto.Name))
            {
                return Conflict(new { message = "Organiser name is already in use." });
            }

            var newUser = new Organiser { Name = dto.Name };
            newUser.Password = _organiserHasher.HashPassword(newUser, dto.Password);

            _dbContext.Organiser.Add(newUser);
            await _dbContext.SaveChangesAsync();

            return StatusCode(201, new { message = "Organiser registered successfully" });
        }

        // ==========================================
        // 2. LOGIN
        // ==========================================

        [HttpPost("organisers/login")]
        public async Task<IActionResult> OrganiserLogin([FromBody] LoginDto dto)
        {
            var user = await _dbContext.Organiser.FirstOrDefaultAsync(u => u.Name == dto.Name);

            if (user == null) return Unauthorized();

            var result = _organiserHasher.VerifyHashedPassword(user, user.Password, dto.Password);
            if (result == PasswordVerificationResult.Failed) return Unauthorized();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, "Organiser"),
                // FIX: Match the custom claim key used in Get/Update profile methods
                new Claim("OrganiserID", user.OrganiserID.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return Ok(new { Role = "Organiser" });
        }

        [HttpPost("participants/login")]
        public async Task<IActionResult> ParticipantLogin([FromBody] LoginDto dto)
        {
            // Assuming participants log in using their phone number as the identifier
            var user = await _dbContext.Participant.FirstOrDefaultAsync(u => u.PhoneNumber == dto.Name || u.Name == dto.Name);

            if (user == null) return Unauthorized();

            var result = _participantHasher.VerifyHashedPassword(user, user.Password, dto.Password);
            if (result == PasswordVerificationResult.Failed) return Unauthorized();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, "Participant"),
                // FIX: Match the custom claim key used in Get/Update profile methods
                new Claim("ParticipantID", user.ParticipantID.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return Ok(new { Role = "Participant" });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out successfully" });
        }

        // ==========================================
        // 3. ORGANISER PROFILE & MANAGEMENT
        // ==========================================

        [Authorize(Roles = "Organiser")]
        [HttpGet("organisers/me")]
        public async Task<IActionResult> GetOrganiserProfile()
        {
            var claimId = User.FindFirst("OrganiserID")?.Value;

            if (string.IsNullOrEmpty(claimId))
            {
                return Unauthorized(new { message = "Organiser identity could not be found in the session." });
            }

            int organiserId = int.Parse(claimId);

            var organiser = await _dbContext.Organiser
                .Where(o => o.OrganiserID == organiserId)
                .Select(o => new { o.OrganiserID, o.Name })
                .FirstOrDefaultAsync();

            if (organiser == null) return NotFound(new { message = "Organiser profile not found." });

            return Ok(organiser);
        }

        [Authorize(Roles = "Organiser")]
        [HttpPut("organisers/me")]
        public async Task<IActionResult> UpdateOrganiserProfile([FromBody] OrganiserRegisterDto dto)
        {
            var claimId = User.FindFirst("OrganiserID")?.Value;
            if (string.IsNullOrEmpty(claimId)) return Unauthorized(new { message = "Organiser identity could not be found in the session." });

            int id = int.Parse(claimId);
            var organiser = await _dbContext.Organiser.FindAsync(id);

            if (organiser == null) return NotFound(new { message = "Organiser profile not found." });

            try
            {
                if (organiser.Name != dto.Name)
                {
                    // FIX: Use AnyAsync to properly check for name collision excluding current user
                    bool nameExists = await _dbContext.Organiser.AnyAsync(u => u.Name == dto.Name && u.OrganiserID != id);
                    if (nameExists) return Conflict(new { message = "Organiser name is already in use." });

                    organiser.Name = dto.Name;
                }

                if (!string.IsNullOrEmpty(dto.Password))
                {
                    organiser.Password = _organiserHasher.HashPassword(organiser, dto.Password);
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                return Conflict(new { message = "The details provided conflict with existing records or do not meet required attributes." });
            }

            return Ok(new { message = "Organiser profile updated successfully." });
        }

        // ==========================================
        // 4. PARTICIPANT PROFILE & MANAGEMENT
        // ==========================================

        [Authorize(Roles = "Participant")]
        [HttpGet("participants/me")]
        public async Task<IActionResult> GetParticipantProfile()
        {
            var claimId = User.FindFirst("ParticipantID")?.Value;

            if (string.IsNullOrEmpty(claimId))
            {
                return Unauthorized(new { message = "Participant identity could not be found in the session." });
            }

            int participantId = int.Parse(claimId);

            // FIX: Query the Participant table instead of Organiser
            var participant = await _dbContext.Participant
                .Where(p => p.ParticipantID == participantId)
                .Select(p => new { p.ParticipantID, p.Name, p.PhoneNumber, p.Age, p.AvatarUrl })
                .FirstOrDefaultAsync();

            if (participant == null) return NotFound(new { message = "Participant profile not found." });

            return Ok(participant);
        }

        [Authorize(Roles = "Participant")]
        [HttpPut("participants/me")]
        public async Task<IActionResult> UpdateParticipantProfile([FromBody] ParticipantRegisterDto dto)
        {
            var claimId = User.FindFirst("ParticipantID")?.Value;
            if (string.IsNullOrEmpty(claimId)) return Unauthorized(new { message = "Participant identity could not be found in the session." });

            int id = int.Parse(claimId);
            var participant = await _dbContext.Participant.FindAsync(id);

            if (participant == null) return NotFound(new { message = "Participant profile not found." });

            try
            {
                if (dto.AvatarUrl != null)
                    participant.AvatarUrl = dto.AvatarUrl;

                if (dto.Age != 0)
                    participant.Age = dto.Age;

                Regex regex = new Regex(@"^\d{10}$"); // Adjusted regex to standard 10-digit format check
                if (!string.IsNullOrEmpty(dto.PhoneNumber))
                {
                    if (regex.IsMatch(dto.PhoneNumber))
                    {
                        // Check if phone number is taken by another participant
                        bool phoneTaken = await _dbContext.Participant.AnyAsync(p => p.PhoneNumber == dto.PhoneNumber && p.ParticipantID != id);
                        if (phoneTaken) return Conflict(new { message = "Phone number is already in use by another participant." });

                        participant.PhoneNumber = dto.PhoneNumber;
                    }
                    else
                    {
                        return Conflict(new { message = "Phone number must be of the format 0833945107" });
                    }
                }

                if (!string.IsNullOrEmpty(dto.Password))
                {
                    participant.Password = _participantHasher.HashPassword(participant, dto.Password);
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                return Conflict(new { message = "The details provided conflict with existing records or do not meet required attributes." });
            }

            return Ok(new { message = "Participant profile updated successfully." });
        }
    }
}