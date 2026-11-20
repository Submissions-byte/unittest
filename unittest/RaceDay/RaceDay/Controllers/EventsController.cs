using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Models;
using RaceDay.Database;
using Microsoft.AspNetCore.Authorization;

[Route("api/[controller]")]
[ApiController]
public class EventsController : ControllerBase
{
    private readonly RaceDayDBContext _context;
    public EventsController(RaceDayDBContext context)
    {
        _context = context;
    }

    // GET: api/Event
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetEvent()
    {
        return Ok(await _context.Event.ToListAsync());
    }

    // GET: api/Event/5
    [HttpGet("{eventid}")]
    public async Task<ActionResult<Event>> GetEvent(int eventid)
    {
        var @event = await _context.Event.FindAsync(eventid);

        if (@event == null)
        {
            return NotFound();
        }

        return Ok(@event);
    }

    // PUT: api/Event/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [Authorize(Roles = "Organiser")]
    [HttpPut("{eventid}/update")]
    public async Task<IActionResult> PutEvent(int? eventid, [FromBody] Event @event)
    {
        if (eventid != @event.EventID)
        {
            return BadRequest(new { message = "Event Id mismatch." });
        }

        var existing = await _context.Event.FindAsync(eventid);

        if (existing == null)
        {
            return NotFound(new { message = $"Event not found." });
        }

        try
        {
            // Update properties on the tracked entity instead of replacing the state entry
            existing.Title = @event.Title;
            existing.EventDate = @event.EventDate;
            existing.StartTime = @event.StartTime;
            existing.EndTime = @event.EndTime;
            existing.EventType = @event.EventType;
            existing.LocationID = @event.LocationID;
            existing.OrganiserID = @event.OrganiserID;
            existing.BannerUrl = @event.BannerUrl;
            existing.Description = @event.Description;

            await _context.SaveChangesAsync();
            return Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EventExists(eventid))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            return Conflict(new { message = $"Invalid data format used for the Event class/entity.\n{ex.Message}" });
        }
    }

    // POST: api/Event
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [Authorize(Roles = "Organiser")]
    [HttpPost]
    public async Task<ActionResult<Event>> PostEvent([FromBody]Event @event)
    {
        var claimId = User.FindFirst("OrganiserID")?.Value;
        if (string.IsNullOrEmpty(claimId)) return Unauthorized(new { message = "Organiser identity could not be found in the session." });

        int id = int.Parse(claimId);
        @event.OrganiserID = id;
        try
        {
            _context.Event.Add(@event);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetEvent", new { eventid = @event.EventID }, @event);
        }catch(Exception ex)
        {
            return Conflict(new { message = $"Invalid data format used for the Event class/entity.\n{ex.Message}" });
        }
    }

    // DELETE: api/Event/5
    [Authorize(Roles ="Organiser")]
    [HttpDelete("{eventid}/delete")]
    public async Task<IActionResult> DeleteEvent(int? eventid)
    {
        var @event = await _context.Event.FindAsync(eventid);
        if (@event == null)
        {
            return NotFound();
        }

        try
        {
            _context.Event.Remove(@event);
            await _context.SaveChangesAsync();

            return NoContent();
        }catch(Exception ex)
        {
            return Conflict(new { message = $"Failed to delete. The object may be in use in other tables.\n{ex.Message}" });
        }
    }

    [Authorize(Roles = "Organiser")]
    [HttpGet("{eventid}/enrolments")]
    public async Task<IActionResult> Enrolments(int? eventid)
    {
        var @event = await _context.Event.FindAsync(eventid);
        if (@event == null)
        {
            return NotFound();
        }

        var enrolments = await _context.Enrolment.Where(u => u.EventID == eventid).ToListAsync();

        return Ok(enrolments);
    }

    //[Authorize(Roles = "Participant")]
    //[HttpPost("{eventid}/enrolments")]
    //public async Task<IActionResult> EnterEnrolments(int? eventid)
    //{
    //    var @event = await _context.Event.FindAsync(eventid);
    //    if (@event == null)
    //    {
    //        return NotFound();
    //    }

    //    var enrolments = await _context.Enrolment.Where(u => u.EventID == eventid).ToListAsync();

    //    return Ok(enrolments);
    //}

    //[Authorize(Roles = "Organiser")]
    //[HttpPost("{eventid}/results")]
    //public async Task<IActionResult> Results(int? eventid)
    //{
    //    var @event = await _context.Results.FindAsync(eventid);
    //    if (@event == null)
    //    {
    //        return NotFound();
    //    }

    //    var results = await _context.Results.Where(u => u.EventID == eventid).ToListAsync();

    //    await _context.SaveChangesAsync();

    //    return NoContent();
    //}

    //Results are posted through the results api

    private bool EventExists(int? eventid)
    {
        return _context.Event.Any(e => e.EventID == eventid);
    }
}
