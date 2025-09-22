using System.Security.Claims;
using Caskr.server;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers
{
    public class BarrelsController(IBarrelsService barrelsService, IUsersService usersService) : AuthorizedApiControllerBase
    {
        [HttpGet("company/{companyId}")]
        public async Task<ActionResult<IEnumerable<Barrel>>> GetBarrelsForCompany(int companyId)
        {
            var user = await GetCurrentUserAsync();
            if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
            {
                return Forbid();
            }

            var barrels = await barrelsService.GetBarrelsForCompanyAsync(companyId);
            return Ok(barrels.ToList());
        }

        [HttpGet("company/{companyId}/forecast")]
        public async Task<ActionResult<object>> Forecast(int companyId, [FromQuery] DateTime targetDate, [FromQuery] int ageYears)
        {
            var user = await GetCurrentUserAsync();
            if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
            {
                return Forbid();
            }

            var barrels = await barrelsService.ForecastBarrelsAsync(companyId, targetDate, ageYears);
            var list = barrels.ToList();
            return Ok(new { barrels = list, count = list.Count });
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
