using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LabelsController(ILabelsService labelsService) : ControllerBase
{
    [HttpPost("ttb-form")]
    public async Task<IActionResult> GenerateTtbForm([FromBody] LabelRequest request)
    {
        var pdf = await labelsService.GenerateTtbFormAsync(request);
        return File(pdf, "application/pdf", "ttb_form_5100_31.pdf");
    }
}
