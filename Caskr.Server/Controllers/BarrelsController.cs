using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BarrelsController(IBarrelsService barrelsService) : ControllerBase
    {
        [HttpGet("company/{companyId}")]
        public async Task<ActionResult<IEnumerable<Barrel>>> GetBarrelsForCompany(int companyId)
        {
            var barrels = await barrelsService.GetBarrelsForCompanyAsync(companyId);
            return Ok(barrels.ToList());
        }

        [HttpGet("company/{companyId}/forecast")]
        public async Task<ActionResult<object>> Forecast(int companyId, [FromQuery] DateTime targetDate, [FromQuery] int ageYears)
        {
            var barrels = await barrelsService.ForecastBarrelsAsync(companyId, targetDate, ageYears);
            var list = barrels.ToList();
            return Ok(new { barrels = list, count = list.Count });
        }
    }
}
