using Caskr.server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Caskr.server.Controllers
{
    public class SpiritTypesController(CaskrDbContext dbContext) : AuthorizedApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SpiritType>>> GetSpiritTypes()
        {
            var spiritTypes = await dbContext.SpiritTypes.AsNoTracking().ToListAsync();
            return Ok(spiritTypes);
        }
    }
}
