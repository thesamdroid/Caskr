using System.Security.Claims;
using Caskr.server;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Controllers
{
    public class UsersController(IUsersService usersService, ILogger<UsersController> logger) : AuthorizedApiControllerBase
    {
        private readonly IUsersService _usersService = usersService;
        private readonly ILogger<UsersController> _logger = logger;

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = (await _usersService.GetUsersAsync()).ToList();
            return Ok(users);
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _usersService.GetUserByIdAsync(id);

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

            var updatedUser = await _usersService.UpdateUserAsync(user);
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
            _logger.LogInformation("Attempting to create user {Email} for company {CompanyId}", user.Email, user.CompanyId);
            try
            {
                var createdUser = await _usersService.AddUserAsync(user);
                _logger.LogInformation(
                    "Successfully created user {Email} with id {UserId} for company {CompanyId}",
                    createdUser.Email,
                    createdUser.Id,
                    createdUser.CompanyId);
                return CreatedAtAction("GetUser", new { id = createdUser.Id }, createdUser);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to create user {Email} for company {CompanyId} due to a conflict",
                    user.Email,
                    user.CompanyId);
                return Conflict();
            }
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _usersService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await _usersService.DeleteUserAsync(id);

            return NoContent();
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            return await GetCurrentUserAsync(_usersService);
        }
    }
}

