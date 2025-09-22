using Caskr.server.Models;
using Caskr.server.Services;
using UserTypeModel = Caskr.server.Models.UserType;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers
{
    public class UserTypesController(IUserTypesService userTypesService) : AuthorizedApiControllerBase
    {
        // GET: api/UserTypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserTypeModel>>> GetUserTypes()
        {
            var userTypes = (await userTypesService.GetUserTypesAsync()).ToList();
            return Ok(userTypes);
        }

        // GET: api/UserTypes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserTypeModel>> GetUserType(int id)
        {
            var userType = await userTypesService.GetUserTypeAsync(id);

            if (userType == null)
            {
                return NotFound();
            }

            return Ok(userType);
        }

        // PUT: api/UserTypes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<ActionResult<UserTypeModel>> PutUserType(int id, UserTypeModel userType)
        {
            if (id != userType.Id)
            {
                return BadRequest();
            }
            var updatedUserType = await userTypesService.UpdateUserTypeAsync(userType);
            return Ok(updatedUserType);
        }

        // POST: api/UserTypes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<UserTypeModel>> PostUserType(UserTypeModel? userType)
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
