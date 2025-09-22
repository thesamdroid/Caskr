using Caskr.server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Caskr.server.Controllers
{
    public class MashBillsController(CaskrDbContext dbContext) : AuthorizedApiControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MashBill>>> GetMashBills()
        {
            var mashBills = await dbContext.MashBills.AsNoTracking().ToListAsync();
            return Ok(mashBills);
        }
    }
}
