using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Models;
using RaceDay.Database;
using Microsoft.AspNetCore.Authorization;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDBContext _context;
    public CategoriesController(RaceDayDBContext context)
    {
        _context = context;
    }

    // GET: api/Category
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategory()
    {
        return Ok(await _context.Category.ToListAsync());
    }

    // GET: api/Category/5
    [HttpGet("{categoryid}")]
    public async Task<ActionResult<Category>> GetCategory(int categoryid)
    {
        var category = await _context.Category.FindAsync(categoryid);

        if (category == null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    // PUT: api/Category/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [Authorize(Roles ="Organiser")]
    [HttpPut("{categoryid}")]
    public async Task<IActionResult> PutCategory(int? categoryid,[FromBody] Category category)
    {
        if (categoryid != category.CategoryID)
        {
            return BadRequest();
        }

    //    if(string.IsNullOrEmpty(category.AgeRange))

        _context.Entry(category).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            return Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CategoryExists(categoryid))
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

    // POST: api/Category
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [Authorize(Roles ="Organiser")]
    [HttpPost("{eventID}/create")]
    public async Task<IActionResult> PostCategory(int? eventID,[FromBody] Category category)
    {
        if (eventID != category.EventID)
        {
            if (eventID == null)
                return NotFound();
            if (_context.Event.FirstOrDefault(u => u.EventID == eventID) == null)
                return NotFound();
        }
        if (category.CategoryID != null)
        {
            if (_context.Category.Find(category.CategoryID) != null)
                return Conflict(new { message = $"Id already in use." });
        }

        if (string.IsNullOrEmpty(category.AgeRange) && (string.IsNullOrEmpty(category.Distance)))
                return BadRequest(new {message = "The category must be defined by either a distance or age range."});

        try
        {
            _context.Category.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCategory", new { categoryid = category.CategoryID }, category);
        }
        catch (Exception ex)
        {
            return Conflict(new { message = $"Invalid data format used for the Category class/entity.\n{ex.Message}" });
        }
    }

    // DELETE: api/Category/5
    [Authorize(Roles ="Organiser")]
    [HttpDelete("{categoryid}")]
    public async Task<IActionResult> DeleteCategory(int? categoryid)
    {
        var category = await _context.Category.FindAsync(categoryid);
        if (category == null)
        {
            return NotFound();
        }
        try
        {
            _context.Category.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return Conflict(new { message = $"Failed to delete. The object may be in use in other tables.\n{ex.Message}" });
        }
    }

    private bool CategoryExists(int? categoryid)
    {
        return _context.Category.Any(e => e.CategoryID == categoryid);
    }
}
