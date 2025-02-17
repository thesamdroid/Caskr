using Caskr.server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController(CaskrDbContext dbContext) : ControllerBase
    {
        private readonly CaskrDbContext _dbContext = dbContext;

        // GET: api/Status
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Status>>> GetStatuses()
        {
            return await _dbContext.Statuses.ToListAsync();
        }

        // GET: api/Status/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Status>> GetStatus(int id)
        {
            var status = await _dbContext.Statuses.FindAsync(id);

            if (status == null)
            {
                return NotFound();
            }

            return status;
        }

        // PUT: api/Status/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStatus(int id, Status status)
        {
            if (id != status.Id)
            {
                return BadRequest();
            }

            _dbContext.Entry(status).State = EntityState.Modified;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StatusExists(id))
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

        // POST: api/Status
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Status>> PostStatus(Status? status)
        {
            _dbContext.Statuses.Add(status);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction("GetStatus", new { id = status.Id }, status);
        }

        // DELETE: api/Status/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStatus(int id)
        {
            var status = await _dbContext.Statuses.FindAsync(id);
            if (status == null)
            {
                return NotFound();
            }

            _dbContext.Statuses.Remove(status);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        private bool StatusExists(int id)
        {
            return _dbContext.Statuses.Any(e => e.Id == id);
        }
    }
}
