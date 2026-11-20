using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Models;
using RaceDay.Database;

[Route("api/[controller]")]
[ApiController]
public class ParticipantsController : ControllerBase
{
    private readonly RaceDayDBContext _context;
    public ParticipantsController(RaceDayDBContext context)
    {
        _context = context;
    }

    // GET: api/Participant
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Participant>>> GetParticipant()
    {
        return await _context.Participant.ToListAsync();
    }

    // GET: api/Participant/5
    [HttpGet("{participantid}")]
    public async Task<ActionResult<Participant>> GetParticipant(int participantid)
    {
        var participant = await _context.Participant.FindAsync(participantid);

        if (participant == null)
        {
            return NotFound();
        }

        return participant;
    }

    // PUT: api/Participant/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{participantid}")]
    public async Task<IActionResult> PutParticipant(int? participantid, Participant participant)
    {
        if (participantid != participant.ParticipantID)
        {
            return BadRequest();
        }

        _context.Entry(participant).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ParticipantExists(participantid))
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

    // POST: api/Participant
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Participant>> PostParticipant(Participant participant)
    {
        _context.Participant.Add(participant);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetParticipant", new { participantid = participant.ParticipantID }, participant);
    }

    // DELETE: api/Participant/5
    [HttpDelete("{participantid}")]
    public async Task<IActionResult> DeleteParticipant(int? participantid)
    {
        var participant = await _context.Participant.FindAsync(participantid);
        if (participant == null)
        {
            return NotFound();
        }

        _context.Participant.Remove(participant);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ParticipantExists(int? participantid)
    {
        return _context.Participant.Any(e => e.ParticipantID == participantid);
    }
}
