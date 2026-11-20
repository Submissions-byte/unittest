using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Models;
using RaceDay.Database;

[Route("api/[controller]")]
[ApiController]
public class OrganisersController : ControllerBase
{
    private readonly RaceDayDBContext _context;
    public OrganisersController(RaceDayDBContext context)
    {
        _context = context;
    }

    // GET: api/Organiser
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Organiser>>> GetOrganiser()
    {
        return await _context.Organiser.ToListAsync();
    }

    // GET: api/Organiser/5
    [HttpGet("{organiserid}")]
    public async Task<ActionResult<Organiser>> GetOrganiser(int organiserid)
    {
        var organiser = await _context.Organiser.FindAsync(organiserid);

        if (organiser == null)
        {
            return NotFound();
        }

        return organiser;
    }

    // PUT: api/Organiser/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{organiserid}")]
    public async Task<IActionResult> PutOrganiser(int? organiserid, Organiser organiser)
    {
        if (organiserid != organiser.OrganiserID)
        {
            return BadRequest();
        }

        _context.Entry(organiser).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!OrganiserExists(organiserid))
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

    // POST: api/Organiser
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Organiser>> PostOrganiser(Organiser organiser)
    {
        _context.Organiser.Add(organiser);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetOrganiser", new { organiserid = organiser.OrganiserID }, organiser);
    }

    // DELETE: api/Organiser/5
    [HttpDelete("{organiserid}")]
    public async Task<IActionResult> DeleteOrganiser(int? organiserid)
    {
        var organiser = await _context.Organiser.FindAsync(organiserid);
        if (organiser == null)
        {
            return NotFound();
        }

        _context.Organiser.Remove(organiser);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool OrganiserExists(int? organiserid)
    {
        return _context.Organiser.Any(e => e.OrganiserID == organiserid);
    }
}
