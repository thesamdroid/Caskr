using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caskr.server.Controllers;

/// <summary>
/// Controller for generating TTB transfer documents
/// </summary>
public class TransfersController(
    ITransfersService transfersService,
    ILogger<TransfersController> logger) : AuthorizedApiControllerBase
{
    /// <summary>
    /// Generate TTB Transfer Form 5100.16
    /// </summary>
    /// <param name="request">Transfer request with shipper, consignee, and barrel information</param>
    /// <returns>PDF file for download</returns>
    [HttpPost("ttb-form")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateTtbForm([FromBody] TransferRequest request)
    {
        try
        {
            // Model validation is automatic with [Required] attributes
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid transfer request: {Errors}", ModelState.Values.SelectMany(v => v.Errors));
                return BadRequest(new { error = "Invalid request data", details = ModelState });
            }

            logger.LogInformation("Generating TTB transfer form for FromCompanyId: {FromCompanyId}", request.FromCompanyId);

            var pdf = await transfersService.GenerateTtbFormAsync(request);

            return File(pdf, "application/pdf", "ttb_form_5100_16.pdf");
        }
        catch (ArgumentNullException ex)
        {
            logger.LogWarning(ex, "Null argument in transfer generation request");
            return BadRequest(new { error = "Invalid request", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid argument in transfer generation request");
            return BadRequest(new { error = "Invalid request data", message = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError(ex, "PDF template file not found");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Template not found", message = "PDF template file is missing. Please contact support." });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Error generating PDF");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "PDF generation failed", message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error generating TTB transfer form");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Internal server error", message = "An unexpected error occurred. Please try again later." });
        }
    }
}
