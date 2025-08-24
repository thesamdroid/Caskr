using Caskr.server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Caskr.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MashBillsController(CaskrDbContext dbContext) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MashBill>>> GetMashBills()
        {
            var mashBills = await dbContext.MashBills.AsNoTracking().ToListAsync();
            return Ok(mashBills);
        }
    }
}
