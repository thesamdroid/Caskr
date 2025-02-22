using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController(IStatusService statusService) : ControllerBase
    {
        // GET: api/Status
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Status>>> GetStatuses()
        {
            var statuses = (await statusService.GetStatusesAsync()).ToList();
            return Ok(statuses);
        }

        // GET: api/Status/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Status>> GetStatus(int id)
        {
            var status = await statusService.GetStatusAsync(id);

            if (status == null)
            {
                return NotFound();
            }

            return Ok(status);
        }

        // PUT: api/Status/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<ActionResult<Status>> PutStatus(int id, Status status)
        {
            if (id != status.Id)
            {
                return BadRequest();
            }

            var updatedStatus = await statusService.UpdateStatusAsync(status);
            return Ok(updatedStatus);
        }

        // POST: api/Status
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Status>> PostStatus(Status? status)
        {
            var createdStatus = await statusService.AddStatusAsync(status);

            return CreatedAtAction("GetStatus", new { id = createdStatus.Id }, createdStatus);
        }

        // DELETE: api/Status/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStatus(int id)
        {
            var status = await statusService.GetStatusAsync(id);
            if (status == null)
            {
                return NotFound();
            }

            await statusService.DeleteStatusAsync(id);

            return NoContent();
        }
    }
}
