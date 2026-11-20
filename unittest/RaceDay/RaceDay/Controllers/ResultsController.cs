using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Models;
using RaceDay.Database;
using Results = RaceDay.Models.Results;
using Microsoft.AspNetCore.Authorization;

[Route("api/[controller]")]
[ApiController]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDBContext _context;
    public ResultsController(RaceDayDBContext context)
    {
        _context = context;
    }

    // GET: api/Results
   // [Authorize(Roles ="Organiser")]
    [HttpGet]
    public async Task<IActionResult> GetResults()
    {
        return  Ok(await _context.Results.ToListAsync());
    }

    // GET: api/Results/5
    [Authorize(Roles = "Organiser")]
    [HttpGet("{resultid}")]
    public async Task<ActionResult<Results>> GetResults(int resultid)
    {
        var results = await _context.Results.FindAsync(resultid);

        if (results == null)
        {
            return NotFound();
        }

        return results;
    }

    // PUT: api/Results/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [Authorize(Roles = "Organiser")]
    [HttpPut("{resultid}")]
    public async Task<IActionResult> PutResults(int? resultid, Results results)
    {
        if (resultid != results.ResultID)
        {
            return BadRequest();
        }

        _context.Entry(results).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ResultsExists(resultid))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/Results
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [Authorize(Roles = "Organiser")]
    [HttpPost]
    public async Task<IActionResult> PostResults(Results results)
    {
        _context.Results.Add(results);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetResults", new { resultid = results.ResultID }, results);
    }

    // DELETE: api/Results/5
    [Authorize(Roles = "Organiser")]
    [HttpDelete("{resultid}")]
    public async Task<IActionResult> DeleteResults(int? resultid)
    {
        var results = await _context.Results.FindAsync(resultid);
        if (results == null)
        {
            return NotFound();
        }

        _context.Results.Remove(results);
        await _context.SaveChangesAsync();

        return NoContent();
    }

//    [Authorize(Roles = "Organiser")]
    [HttpGet("~/api/events/{eventid}/results")]
    public async Task<IActionResult> GetEventResults(int eventid)
    {
        var enrolments = await _context.Enrolment.Where(u => u.EventID == eventid).Select(u=>u.EnrolmentID).ToListAsync();
        var results = await _context.Results.Where(u=>enrolments.Contains(u.EnrolmentID)).ToListAsync();

        if (results == null)
        {
            return NotFound();
        }

        return Ok(results);
    }

    [Authorize(Roles ="Participant")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyResults()
    {
        var claimId = User.FindFirst("ParticipantID")?.Value;
        if (string.IsNullOrEmpty(claimId)) return Unauthorized(new { message = "Participant identity could not be found in the session." });

        int id = int.Parse(claimId);

        var results = await _context.Results.Where(u => u.enrolment.ParticipantID == id).ToListAsync();

        if (results.Count==0)
        {
            return NotFound("You have no recorded results.");
        }

        return Ok(results);
    }

    private bool ResultsExists(int? resultid)
    {
        return _context.Results.Any(e => e.ResultID == resultid);
    }
}
