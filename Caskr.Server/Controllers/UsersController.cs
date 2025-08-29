using System.Security.Claims;
using Caskr.server;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController(IUsersService usersService) : ControllerBase
    {
        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = (await usersService.GetUsersAsync()).ToList();
            return Ok(users);
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await usersService.GetUserByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<ActionResult<User>> PutUser(int id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            var updatedUser = await usersService.UpdateUserAsync(user);
            return Ok(updatedUser);
        }

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User? user)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser is null || ((UserType)currentUser.UserTypeId != UserType.Admin && (UserType)currentUser.UserTypeId != UserType.SuperAdmin))
            {
                return Forbid();
            }

            if (user is null)
            {
                return BadRequest();
            }

            user.CompanyId = currentUser.CompanyId;
            try
            {
                var createdUser = await usersService.AddUserAsync(user);
                return CreatedAtAction("GetUser", new { id = createdUser.Id }, createdUser);
            }
            catch (InvalidOperationException)
            {
                return Conflict();
            }
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await usersService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await usersService.DeleteUserAsync(id);

            return NoContent();
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return null;
            }

            return await usersService.GetUserByIdAsync(userId);
        }
    }
}

