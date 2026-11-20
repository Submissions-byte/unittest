using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Database;
using RaceDay.Models;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDBContext _context;
    public EnrolmentsController(RaceDayDBContext context)
    {
        _context = context;
    }

    // GET: api/Enrolment
    [Authorize(Roles ="Organiser")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Enrolment>>> GetEnrolment()
    {
        return Ok(await _context.Enrolment.ToListAsync());
    }

    // GET: api/Enrolment
    [Authorize(Roles = "Organiser")]
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<Enrolment>>> GetMyEventEnrolment()
    {
        var ClaimID = User.FindFirst("OrganiserID")?.Value;
        if (string.IsNullOrEmpty(ClaimID)) return Unauthorized(new { message = "Organiser identity could not be found in the session." });

        int id = int.Parse(ClaimID);
        //var organiser = await _context.Organiser.FindAsync(id);

        var events = await _context.Event.Where(u=> u.OrganiserID==id).Select(u=>u.EventID).ToListAsync();

        var output = await _context.Enrolment.Where(u => events.Contains(u.EventID)).ToListAsync();

        return Ok(output);
    }

    // GET: api/Enrolment/5
    [Authorize(Roles = "Organiser")]
    [HttpGet("{enrolmentid}")]
    public async Task<ActionResult<Enrolment>> GetEnrolment(int enrolmentid)
    {
        var enrolment = await _context.Enrolment.FindAsync(enrolmentid);

        if (enrolment == null)
        {
            return NotFound();
        }

        return Ok(enrolment);
    }

    // PUT: api/Enrolment/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [Authorize(Roles = "Organiser")]
    [HttpPut("{enrolmentid}")]
    public async Task<IActionResult> PutEnrolment(int? enrolmentid,[FromBody] Enrolment enrolment)
    {
        if (enrolmentid != enrolment.EnrolmentID)
        {
            return BadRequest();
        }
        try
        {
            _context.Entry(enrolment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EnrolmentExists(enrolmentid))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            return Conflict(new { message = $"Invalid data format used for the Enrolment class/entity.\n{ex.Message}" });
        }

        return NoContent();
    }

    // POST: api/Enrolment
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [Authorize(Roles = "Organiser")]
    [HttpPost]
    public async Task<ActionResult<Enrolment>> PostEnrolment([FromBody]Enrolment enrolment)
    {
        try
        {
            _context.Enrolment.Add(enrolment);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetEnrolment", new { enrolmentid = enrolment.EnrolmentID }, enrolment);
        }
        catch (Exception ex)
        {
            return Conflict(new { message = $"Invalid data format used for the Enrolment class/entity.\n{ex.Message}" });
        }
    }

    // DELETE: api/Enrolment/5
    [Authorize(Roles ="Organiser")]
    [HttpDelete("{enrolmentid}")]
    public async Task<IActionResult> DeleteEnrolment(int? enrolmentid)
    {
        var enrolment = await _context.Enrolment.FindAsync(enrolmentid);
        if (enrolment == null)
        {
            return NotFound();
        }

        try
        {
            _context.Enrolment.Remove(enrolment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return Conflict(new { message = $"Failed to delete. The object may be in use in the results table.\n{ex.Message}" });
        }
    }

    private bool EnrolmentExists(int? enrolmentid)
    {
        return _context.Enrolment.Any(e => e.EnrolmentID == enrolmentid);
    }

    [Authorize(Roles = "Participant")]
    [HttpPost("ParticipantEnrolment")]
    public async Task<ActionResult<Enrolment>> ParticipantEnrolmentByCategory([FromBody] EventCategoryDto enrolment)
    {
        var claimId = User.FindFirst("ParticipantID")?.Value;
        if (string.IsNullOrEmpty(claimId)) return Unauthorized(new { message = "Participant identity could not be found in the session." });

        int id = int.Parse(claimId);

        var output = new Enrolment
        {
            ParticipantID = id,
            EnrolmentDate = DateOnly.FromDateTime(DateTime.Now),
            EventID = enrolment.EventID,
            CategoryID = enrolment.CategoryID
        };
        try
        {
            _context.Enrolment.Add(output);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetEnrolment", new { enrolmentid = output.EnrolmentID }, output);
        }
        catch (Exception ex)
        {
            return Conflict(new { message = $"Invalid data format used for the Enrolment class/entity.\n{ex.Message}" });
        }
    }
}
