using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserTypesController(IUserTypesService userTypesService) : ControllerBase
    {
        // GET: api/UserTypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserType>>> GetUserTypes()
        {
            return (await userTypesService.GetUserTypesAsync()).ToList();
        }

        // GET: api/UserTypes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserType>> GetUserType(int id)
        {
            var userType = await userTypesService.GetUserTypeAsync(id);

            if (userType == null)
            {
                return NotFound();
            }

            return userType;
        }

        // PUT: api/UserTypes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<ActionResult<UserType>> PutUserType(int id, UserType userType)
        {
            if (id != userType.Id)
            {
                return BadRequest();
            }
            return await userTypesService.UpdateUserTypeAsync(userType);
        }

        // POST: api/UserTypes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<UserType>> PostUserType(UserType? userType)
        {
            var createdUserType = await userTypesService.AddUserTypeAsync(userType);

            return CreatedAtAction("GetUserType", new { id = createdUserType.Id }, createdUserType);
        }

        // DELETE: api/UserTypes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserType(int id)
        {
            var userType = await userTypesService.GetUserTypeAsync(id);
            if (userType == null)
            {
                return NotFound();
            }

            await userTypesService.DeleteUserTypeAsync(id);

            return NoContent();
        }
    }
}
