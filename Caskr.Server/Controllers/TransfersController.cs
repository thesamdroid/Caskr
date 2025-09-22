using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers;

public class TransfersController(ITransfersService transfersService) : AuthorizedApiControllerBase
{
    [HttpPost("ttb-form")]
    public async Task<IActionResult> GenerateTtbForm([FromBody] TransferRequest request)
    {
        var pdf = await transfersService.GenerateTtbFormAsync(request);
        return File(pdf, "application/pdf", "ttb_form_5100_16.pdf");
    }
}
