using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Models;
using RaceDay.Database;
using Microsoft.AspNetCore.Authorization;

[Route("api/[controller]")]
[ApiController]
public class LocationsController : ControllerBase
{
    private readonly RaceDayDBContext _context;
    public LocationsController(RaceDayDBContext context)
    {
        _context = context;
    }

    // GET: api/Location
   // [Authorize(Roles = "Organiser")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Location>>> GetLocation()
    {
        return Ok(await _context.Location.ToListAsync());
    }

    // GET: api/Location/5
    //[Authorize(Roles = "Organiser")]
    [HttpGet("{locationid}")]
    public async Task<ActionResult<IEnumerable<Location>>> GetLocation(int locationid)
    {
        var location = await _context.Location.FindAsync(locationid);

        if (location == null)
        {
            return NotFound();
        }

        return Ok(location);
    }

    // PUT: api/Location/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [Authorize(Roles = "Organiser")]
    [HttpPut("{locationid}")]
    public async Task<ActionResult<IEnumerable<Location>>> PutLocation(int? locationid, Location location)
    {
        if (locationid != location.LocationID)
        {
            return BadRequest();
        }

        _context.Entry(location).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            return Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!LocationExists(locationid))
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

    // POST: api/Location
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [Authorize(Roles = "Organiser")]
    [HttpPost]
    public async Task<ActionResult<Location>> PostLocation([FromBody]Location location)
    {
        if (_context.Location.Where(u => u.GPS_coordinates == location.GPS_coordinates).FirstOrDefault() != null)
            return Conflict(new { message = "The location identified by these GPS coordinates is already in use." });
        try
        {
            _context.Location.Add(location);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetLocation", new { locationid = location.LocationID }, location);
        }
        catch (Exception ex)
        {
            return Conflict(new { message = $"Invalid data format used for the Location class/entity.\n{ex.Message}" });
        }
    }

    // DELETE: api/Location/5
    [Authorize(Roles ="Organiser")]
    [HttpDelete("{locationid}")]
    public async Task<IActionResult> DeleteLocation(int? locationid)
    {
        var location = await _context.Location.FindAsync(locationid);
        if (location == null)
        {
            return NotFound();
        }
        try
        {
            _context.Location.Remove(location);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return Conflict(new { message = $"Failed to delete. The object may be in use in other tables.\n{ex.Message}" });
        }
    }

    private bool LocationExists(int? locationid)
    {
        return _context.Location.Any(e => e.LocationID == locationid);
    }
}
