using System.Security.Claims;
using Caskr.server.Models;
using Caskr.server.Models.SupplyChain;
using Caskr.server.Services;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Controllers;

#region Request/Response Models

public class PurchaseOrderItemRequest
{
    public int SupplierProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseOrderRequest
{
    public int SupplierId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public List<PurchaseOrderItemRequest> Items { get; set; } = new();
}

public class PurchaseOrderItemResponse
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int SupplierProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PurchaseOrderResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierEmail { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string Status { get; set; } = "Draft";
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentStatus { get; set; } = "Unpaid";
    public string? Notes { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PurchaseOrderItemResponse>? Items { get; set; }
    public int? LineItemCount { get; set; }
    public decimal? TotalQuantityOrdered { get; set; }
    public decimal? TotalQuantityReceived { get; set; }
}

public class InventoryReceiptItemRequest
{
    public int PurchaseOrderItemId { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public ReceiptItemCondition Condition { get; set; } = ReceiptItemCondition.Good;
    public string? Notes { get; set; }
}

public class InventoryReceiptRequest
{
    public int PurchaseOrderId { get; set; }
    public DateTime ReceiptDate { get; set; }
    public string? Notes { get; set; }
    public List<InventoryReceiptItemRequest> Items { get; set; } = new();
}

public class InventoryReceiptItemResponse
{
    public int Id { get; set; }
    public int InventoryReceiptId { get; set; }
    public int PurchaseOrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal ReceivedQuantity { get; set; }
    public string Condition { get; set; } = "Good";
    public string? Notes { get; set; }
}

public class InventoryReceiptResponse
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public DateTime ReceiptDate { get; set; }
    public int? ReceivedByUserId { get; set; }
    public string? ReceivedByUserName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<InventoryReceiptItemResponse> Items { get; set; } = new();
}

public class SendPOEmailRequest
{
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

#endregion

/// <summary>
/// Controller for purchase order management
/// </summary>
public class PurchaseOrdersController : AuthorizedApiControllerBase
{
    private readonly CaskrDbContext _dbContext;
    private readonly ILogger<PurchaseOrdersController> _logger;
    private readonly IEmailService _emailService;

    public PurchaseOrdersController(
        CaskrDbContext dbContext,
        ILogger<PurchaseOrdersController> logger,
        IEmailService emailService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _emailService = emailService;
    }

    /// <summary>
    /// Get all purchase orders for a company
    /// </summary>
    [HttpGet("company/{companyId}")]
    public async Task<ActionResult<IEnumerable<PurchaseOrderResponse>>> GetPurchaseOrders(
        int companyId,
        [FromQuery] PurchaseOrderStatus? status = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        _logger.LogInformation("Fetching purchase orders for company {CompanyId}", companyId);

        var query = _dbContext.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.CreatedByUser)
            .Include(po => po.Items)
            .Where(po => po.CompanyId == companyId);

        if (status.HasValue)
        {
            query = query.Where(po => po.Status == status.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(po => po.SupplierId == supplierId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(po => po.OrderDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(po => po.OrderDate <= endDate.Value);
        }

        var purchaseOrders = await query
            .OrderByDescending(po => po.OrderDate)
            .ThenByDescending(po => po.CreatedAt)
            .Select(po => new PurchaseOrderResponse
            {
                Id = po.Id,
                CompanyId = po.CompanyId,
                SupplierId = po.SupplierId,
                SupplierName = po.Supplier != null ? po.Supplier.SupplierName : "",
                SupplierEmail = po.Supplier != null ? po.Supplier.Email : null,
                PoNumber = po.PoNumber,
                OrderDate = po.OrderDate,
                ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                Status = po.Status.ToString(),
                TotalAmount = po.TotalAmount ?? 0,
                Currency = po.Currency,
                PaymentStatus = po.PaymentStatus.ToString(),
                Notes = po.Notes,
                CreatedByUserId = po.CreatedByUserId,
                CreatedByUserName = po.CreatedByUser != null ? po.CreatedByUser.Name : null,
                CreatedAt = po.CreatedAt,
                UpdatedAt = po.UpdatedAt,
                LineItemCount = po.Items.Count,
                TotalQuantityOrdered = po.Items.Sum(i => i.Quantity),
                TotalQuantityReceived = po.Items.Sum(i => i.ReceivedQuantity)
            })
            .ToListAsync();

        return Ok(purchaseOrders);
    }

    /// <summary>
    /// Get a single purchase order by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PurchaseOrderResponse>> GetPurchaseOrder(int id)
    {
        var po = await _dbContext.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.CreatedByUser)
            .Include(p => p.Items)
                .ThenInclude(i => i.SupplierProduct)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != po.CompanyId))
        {
            return Forbid();
        }

        return Ok(new PurchaseOrderResponse
        {
            Id = po.Id,
            CompanyId = po.CompanyId,
            SupplierId = po.SupplierId,
            SupplierName = po.Supplier?.SupplierName ?? "",
            SupplierEmail = po.Supplier?.Email,
            PoNumber = po.PoNumber,
            OrderDate = po.OrderDate,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            Status = po.Status.ToString(),
            TotalAmount = po.TotalAmount ?? 0,
            Currency = po.Currency,
            PaymentStatus = po.PaymentStatus.ToString(),
            Notes = po.Notes,
            CreatedByUserId = po.CreatedByUserId,
            CreatedByUserName = po.CreatedByUser?.Name,
            CreatedAt = po.CreatedAt,
            UpdatedAt = po.UpdatedAt,
            Items = po.Items.Select(i => new PurchaseOrderItemResponse
            {
                Id = i.Id,
                PurchaseOrderId = i.PurchaseOrderId,
                SupplierProductId = i.SupplierProductId,
                ProductName = i.SupplierProduct?.ProductName ?? "",
                Sku = i.SupplierProduct?.Sku,
                UnitOfMeasure = i.SupplierProduct?.UnitOfMeasure,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                ReceivedQuantity = i.ReceivedQuantity,
                Notes = i.Notes,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            }).ToList()
        });
    }

    /// <summary>
    /// Get next PO number for a company
    /// </summary>
    [HttpGet("company/{companyId}/next-po-number")]
    public async Task<ActionResult<object>> GetNextPoNumber(int companyId)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        var year = DateTime.UtcNow.Year;
        var lastPo = await _dbContext.PurchaseOrders
            .Where(po => po.CompanyId == companyId && po.PoNumber.StartsWith($"PO-{year}-"))
            .OrderByDescending(po => po.PoNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastPo != null)
        {
            var parts = lastPo.PoNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        var poNumber = $"PO-{year}-{nextNumber:D3}";
        return Ok(new { poNumber });
    }

    /// <summary>
    /// Create a new purchase order
    /// </summary>
    [HttpPost("company/{companyId}")]
    public async Task<ActionResult<PurchaseOrderResponse>> CreatePurchaseOrder(
        int companyId,
        [FromBody] PurchaseOrderRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != companyId))
        {
            return Forbid();
        }

        // Validate supplier exists
        var supplier = await _dbContext.Suppliers.FindAsync(request.SupplierId);
        if (supplier is null || supplier.CompanyId != companyId)
        {
            return BadRequest(new { message = "Invalid supplier" });
        }

        // Validate items
        if (request.Items.Count == 0)
        {
            return BadRequest(new { message = "At least one line item is required" });
        }

        // Generate PO number
        var year = DateTime.UtcNow.Year;
        var lastPo = await _dbContext.PurchaseOrders
            .Where(po => po.CompanyId == companyId && po.PoNumber.StartsWith($"PO-{year}-"))
            .OrderByDescending(po => po.PoNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastPo != null)
        {
            var parts = lastPo.PoNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        var poNumber = $"PO-{year}-{nextNumber:D3}";

        var purchaseOrder = new PurchaseOrder
        {
            CompanyId = companyId,
            SupplierId = request.SupplierId,
            PoNumber = poNumber,
            OrderDate = request.OrderDate,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Status = PurchaseOrderStatus.Draft,
            Currency = "USD",
            PaymentStatus = PaymentStatus.Unpaid,
            Notes = request.Notes,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        decimal totalAmount = 0;

        foreach (var itemReq in request.Items)
        {
            var product = await _dbContext.SupplierProducts.FindAsync(itemReq.SupplierProductId);
            if (product is null || product.SupplierId != request.SupplierId)
            {
                return BadRequest(new { message = $"Invalid product ID: {itemReq.SupplierProductId}" });
            }

            var totalPrice = itemReq.Quantity * itemReq.UnitPrice;
            totalAmount += totalPrice;

            purchaseOrder.Items.Add(new PurchaseOrderItem
            {
                SupplierProductId = itemReq.SupplierProductId,
                Quantity = itemReq.Quantity,
                UnitPrice = itemReq.UnitPrice,
                TotalPrice = totalPrice,
                ReceivedQuantity = 0,
                Notes = itemReq.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        purchaseOrder.TotalAmount = totalAmount;

        _dbContext.PurchaseOrders.Add(purchaseOrder);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Created purchase order {PoId} '{PoNumber}' for company {CompanyId}",
            purchaseOrder.Id, purchaseOrder.PoNumber, companyId);

        // Return the created PO
        return CreatedAtAction(nameof(GetPurchaseOrder), new { id = purchaseOrder.Id }, new PurchaseOrderResponse
        {
            Id = purchaseOrder.Id,
            CompanyId = purchaseOrder.CompanyId,
            SupplierId = purchaseOrder.SupplierId,
            SupplierName = supplier.SupplierName,
            SupplierEmail = supplier.Email,
            PoNumber = purchaseOrder.PoNumber,
            OrderDate = purchaseOrder.OrderDate,
            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
            Status = purchaseOrder.Status.ToString(),
            TotalAmount = purchaseOrder.TotalAmount ?? 0,
            Currency = purchaseOrder.Currency,
            PaymentStatus = purchaseOrder.PaymentStatus.ToString(),
            Notes = purchaseOrder.Notes,
            CreatedByUserId = purchaseOrder.CreatedByUserId,
            CreatedByUserName = user.Name,
            CreatedAt = purchaseOrder.CreatedAt,
            UpdatedAt = purchaseOrder.UpdatedAt,
            LineItemCount = purchaseOrder.Items.Count
        });
    }

    /// <summary>
    /// Update a purchase order (only if Draft)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<PurchaseOrderResponse>> UpdatePurchaseOrder(
        int id,
        [FromBody] PurchaseOrderRequest request)
    {
        var po = await _dbContext.PurchaseOrders
            .Include(p => p.Items)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != po.CompanyId))
        {
            return Forbid();
        }

        if (po.Status != PurchaseOrderStatus.Draft)
        {
            return BadRequest(new { message = "Only draft purchase orders can be edited" });
        }

        // Clear existing items
        _dbContext.PurchaseOrderItems.RemoveRange(po.Items);

        // Add new items
        decimal totalAmount = 0;

        foreach (var itemReq in request.Items)
        {
            var product = await _dbContext.SupplierProducts.FindAsync(itemReq.SupplierProductId);
            if (product is null || product.SupplierId != po.SupplierId)
            {
                return BadRequest(new { message = $"Invalid product ID: {itemReq.SupplierProductId}" });
            }

            var totalPrice = itemReq.Quantity * itemReq.UnitPrice;
            totalAmount += totalPrice;

            po.Items.Add(new PurchaseOrderItem
            {
                SupplierProductId = itemReq.SupplierProductId,
                Quantity = itemReq.Quantity,
                UnitPrice = itemReq.UnitPrice,
                TotalPrice = totalPrice,
                ReceivedQuantity = 0,
                Notes = itemReq.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        po.OrderDate = request.OrderDate;
        po.ExpectedDeliveryDate = request.ExpectedDeliveryDate;
        po.Notes = request.Notes;
        po.TotalAmount = totalAmount;
        po.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated purchase order {PoId} '{PoNumber}'", po.Id, po.PoNumber);

        return Ok(new PurchaseOrderResponse
        {
            Id = po.Id,
            CompanyId = po.CompanyId,
            SupplierId = po.SupplierId,
            SupplierName = po.Supplier?.SupplierName ?? "",
            SupplierEmail = po.Supplier?.Email,
            PoNumber = po.PoNumber,
            OrderDate = po.OrderDate,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            Status = po.Status.ToString(),
            TotalAmount = po.TotalAmount ?? 0,
            Currency = po.Currency,
            PaymentStatus = po.PaymentStatus.ToString(),
            Notes = po.Notes,
            CreatedByUserId = po.CreatedByUserId,
            CreatedAt = po.CreatedAt,
            UpdatedAt = po.UpdatedAt,
            LineItemCount = po.Items.Count
        });
    }

    /// <summary>
    /// Send purchase order to supplier (changes status to Sent)
    /// </summary>
    [HttpPost("{id}/send")]
    public async Task<ActionResult<PurchaseOrderResponse>> SendPurchaseOrder(int id)
    {
        var po = await _dbContext.PurchaseOrders
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != po.CompanyId))
        {
            return Forbid();
        }

        if (po.Status != PurchaseOrderStatus.Draft)
        {
            return BadRequest(new { message = "Only draft purchase orders can be sent" });
        }

        po.Status = PurchaseOrderStatus.Sent;
        po.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Sent purchase order {PoId} '{PoNumber}'", po.Id, po.PoNumber);

        return Ok(new PurchaseOrderResponse
        {
            Id = po.Id,
            CompanyId = po.CompanyId,
            SupplierId = po.SupplierId,
            SupplierName = po.Supplier?.SupplierName ?? "",
            PoNumber = po.PoNumber,
            OrderDate = po.OrderDate,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            Status = po.Status.ToString(),
            TotalAmount = po.TotalAmount ?? 0,
            Currency = po.Currency,
            PaymentStatus = po.PaymentStatus.ToString(),
            Notes = po.Notes,
            CreatedAt = po.CreatedAt,
            UpdatedAt = po.UpdatedAt
        });
    }

    /// <summary>
    /// Send PO via email
    /// </summary>
    [HttpPost("{id}/email")]
    public async Task<ActionResult<PurchaseOrderResponse>> EmailPurchaseOrder(
        int id,
        [FromBody] SendPOEmailRequest request)
    {
        var po = await _dbContext.PurchaseOrders
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != po.CompanyId))
        {
            return Forbid();
        }

        // Send email
        await _emailService.SendEmailAsync(request.ToEmail, request.Subject, request.Body);

        // Update status if Draft
        if (po.Status == PurchaseOrderStatus.Draft)
        {
            po.Status = PurchaseOrderStatus.Sent;
            po.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        _logger.LogInformation("Emailed purchase order {PoId} '{PoNumber}' to {Email}", po.Id, po.PoNumber, request.ToEmail);

        return Ok(new PurchaseOrderResponse
        {
            Id = po.Id,
            CompanyId = po.CompanyId,
            SupplierId = po.SupplierId,
            SupplierName = po.Supplier?.SupplierName ?? "",
            PoNumber = po.PoNumber,
            OrderDate = po.OrderDate,
            Status = po.Status.ToString(),
            TotalAmount = po.TotalAmount ?? 0,
            Currency = po.Currency,
            PaymentStatus = po.PaymentStatus.ToString(),
            CreatedAt = po.CreatedAt,
            UpdatedAt = po.UpdatedAt
        });
    }

    /// <summary>
    /// Cancel purchase order
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> CancelPurchaseOrder(int id)
    {
        var po = await _dbContext.PurchaseOrders.FindAsync(id);

        if (po is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != po.CompanyId))
        {
            return Forbid();
        }

        if (po.Status == PurchaseOrderStatus.Received)
        {
            return BadRequest(new { message = "Cannot cancel a fully received purchase order" });
        }

        po.Status = PurchaseOrderStatus.Cancelled;
        po.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Cancelled purchase order {PoId} '{PoNumber}'", po.Id, po.PoNumber);

        return Ok(new { message = "Purchase order cancelled successfully" });
    }

    /// <summary>
    /// Delete purchase order (only if Draft)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePurchaseOrder(int id)
    {
        var po = await _dbContext.PurchaseOrders.FindAsync(id);

        if (po is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != po.CompanyId))
        {
            return Forbid();
        }

        if (po.Status != PurchaseOrderStatus.Draft)
        {
            return BadRequest(new { message = "Only draft purchase orders can be deleted" });
        }

        _dbContext.PurchaseOrders.Remove(po);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted purchase order {PoId} '{PoNumber}'", po.Id, po.PoNumber);

        return Ok(new { message = "Purchase order deleted successfully" });
    }

    #region Inventory Receipts

    /// <summary>
    /// Get receipts for a purchase order
    /// </summary>
    [HttpGet("{id}/receipts")]
    public async Task<ActionResult<IEnumerable<InventoryReceiptResponse>>> GetPurchaseOrderReceipts(int id)
    {
        var po = await _dbContext.PurchaseOrders.FindAsync(id);

        if (po is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != po.CompanyId))
        {
            return Forbid();
        }

        var receipts = await _dbContext.InventoryReceipts
            .Include(r => r.ReceivedByUser)
            .Include(r => r.Items)
                .ThenInclude(i => i.PurchaseOrderItem)
                    .ThenInclude(poi => poi!.SupplierProduct)
            .Where(r => r.PurchaseOrderId == id)
            .OrderByDescending(r => r.ReceiptDate)
            .Select(r => new InventoryReceiptResponse
            {
                Id = r.Id,
                PurchaseOrderId = r.PurchaseOrderId,
                ReceiptDate = r.ReceiptDate,
                ReceivedByUserId = r.ReceivedByUserId,
                ReceivedByUserName = r.ReceivedByUser != null ? r.ReceivedByUser.Name : null,
                Notes = r.Notes,
                CreatedAt = r.CreatedAt,
                Items = r.Items.Select(i => new InventoryReceiptItemResponse
                {
                    Id = i.Id,
                    InventoryReceiptId = i.InventoryReceiptId,
                    PurchaseOrderItemId = i.PurchaseOrderItemId,
                    ProductName = i.PurchaseOrderItem != null && i.PurchaseOrderItem.SupplierProduct != null
                        ? i.PurchaseOrderItem.SupplierProduct.ProductName
                        : "",
                    ReceivedQuantity = i.ReceivedQuantity,
                    Condition = i.Condition.ToString(),
                    Notes = i.Notes
                }).ToList()
            })
            .ToListAsync();

        return Ok(receipts);
    }

    #endregion

    /// <summary>
    /// Generate PDF for purchase order
    /// </summary>
    [HttpGet("{id}/pdf")]
    public async Task<ActionResult> GeneratePdf(int id)
    {
        var po = await _dbContext.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Company)
            .Include(p => p.Items)
                .ThenInclude(i => i.SupplierProduct)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != po.CompanyId))
        {
            return Forbid();
        }

        _logger.LogInformation("Generating PDF for purchase order {PoNumber}", po.PoNumber);

        using var ms = new MemoryStream();
        using (var writer = new PdfWriter(ms))
        using (var pdf = new PdfDocument(writer))
        using (var document = new Document(pdf))
        {
            // Title
            document.Add(new Paragraph("PURCHASE ORDER")
                .SetFontSize(20)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph(po.PoNumber)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("\n"));

            // Company and Supplier Info
            var infoTable = new Table(2).UseAllAvailableWidth();

            // Company (From)
            var companyCell = new Cell()
                .Add(new Paragraph("FROM:").SetBold())
                .Add(new Paragraph(po.Company?.CompanyName ?? ""))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            infoTable.AddCell(companyCell);

            // Supplier (To)
            var supplierCell = new Cell()
                .Add(new Paragraph("TO:").SetBold())
                .Add(new Paragraph(po.Supplier?.SupplierName ?? ""))
                .Add(new Paragraph(po.Supplier?.ContactEmail ?? ""))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            infoTable.AddCell(supplierCell);

            document.Add(infoTable);
            document.Add(new Paragraph("\n"));

            // Order Details
            document.Add(new Paragraph($"Order Date: {po.OrderDate:MMMM dd, yyyy}").SetFontSize(11));
            if (po.ExpectedDeliveryDate.HasValue)
            {
                document.Add(new Paragraph($"Expected Delivery: {po.ExpectedDeliveryDate:MMMM dd, yyyy}").SetFontSize(11));
            }
            document.Add(new Paragraph($"Status: {po.Status}").SetFontSize(11));
            document.Add(new Paragraph("\n"));

            // Items Table
            document.Add(new Paragraph("Order Items").SetBold().SetFontSize(14));

            var itemsTable = new Table(new float[] { 3, 1, 1, 1 }).UseAllAvailableWidth();

            // Header
            itemsTable.AddHeaderCell(new Cell().Add(new Paragraph("Product").SetBold()));
            itemsTable.AddHeaderCell(new Cell().Add(new Paragraph("Qty").SetBold().SetTextAlignment(TextAlignment.CENTER)));
            itemsTable.AddHeaderCell(new Cell().Add(new Paragraph("Unit Price").SetBold().SetTextAlignment(TextAlignment.RIGHT)));
            itemsTable.AddHeaderCell(new Cell().Add(new Paragraph("Total").SetBold().SetTextAlignment(TextAlignment.RIGHT)));

            foreach (var item in po.Items)
            {
                itemsTable.AddCell(new Cell().Add(new Paragraph(item.SupplierProduct?.ProductName ?? "Unknown")));
                itemsTable.AddCell(new Cell().Add(new Paragraph($"{item.Quantity}").SetTextAlignment(TextAlignment.CENTER)));
                itemsTable.AddCell(new Cell().Add(new Paragraph($"{item.UnitPrice:C}").SetTextAlignment(TextAlignment.RIGHT)));
                itemsTable.AddCell(new Cell().Add(new Paragraph($"{item.TotalPrice:C}").SetTextAlignment(TextAlignment.RIGHT)));
            }

            document.Add(itemsTable);
            document.Add(new Paragraph("\n"));

            // Total
            document.Add(new Paragraph($"Total Amount: {po.TotalAmount:C}")
                .SetBold()
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.RIGHT));

            // Notes
            if (!string.IsNullOrWhiteSpace(po.Notes))
            {
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph("Notes:").SetBold());
                document.Add(new Paragraph(po.Notes));
            }
        }

        var pdfBytes = ms.ToArray();
        _logger.LogInformation("Successfully generated PDF for purchase order {PoNumber}. Size: {Size} bytes", po.PoNumber, pdfBytes.Length);

        return File(pdfBytes, "application/pdf", $"{po.PoNumber}.pdf");
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _dbContext.Users.FindAsync(userId);
    }
}

/// <summary>
/// Controller for inventory receipts
/// </summary>
[Route("api/[controller]")]
public class InventoryReceiptsController : AuthorizedApiControllerBase
{
    private readonly CaskrDbContext _dbContext;
    private readonly ILogger<InventoryReceiptsController> _logger;

    public InventoryReceiptsController(CaskrDbContext dbContext, ILogger<InventoryReceiptsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Create an inventory receipt
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<InventoryReceiptResponse>> CreateInventoryReceipt([FromBody] InventoryReceiptRequest request)
    {
        var po = await _dbContext.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId);

        if (po is null)
        {
            return NotFound(new { message = "Purchase order not found" });
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != po.CompanyId))
        {
            return Forbid();
        }

        if (po.Status == PurchaseOrderStatus.Draft || po.Status == PurchaseOrderStatus.Cancelled)
        {
            return BadRequest(new { message = "Cannot receive items for draft or cancelled purchase orders" });
        }

        var receipt = new InventoryReceipt
        {
            PurchaseOrderId = request.PurchaseOrderId,
            ReceiptDate = request.ReceiptDate,
            ReceivedByUserId = user.Id,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var itemReq in request.Items)
        {
            var poItem = po.Items.FirstOrDefault(i => i.Id == itemReq.PurchaseOrderItemId);
            if (poItem is null)
            {
                return BadRequest(new { message = $"Invalid purchase order item ID: {itemReq.PurchaseOrderItemId}" });
            }

            // Update received quantity on PO item
            poItem.ReceivedQuantity += itemReq.ReceivedQuantity;
            poItem.UpdatedAt = DateTime.UtcNow;

            receipt.Items.Add(new InventoryReceiptItem
            {
                PurchaseOrderItemId = itemReq.PurchaseOrderItemId,
                ReceivedQuantity = itemReq.ReceivedQuantity,
                Condition = itemReq.Condition,
                Notes = itemReq.Notes,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Update PO status based on received quantities
        var totalOrdered = po.Items.Sum(i => i.Quantity);
        var totalReceived = po.Items.Sum(i => i.ReceivedQuantity);

        if (totalReceived >= totalOrdered)
        {
            po.Status = PurchaseOrderStatus.Received;
        }
        else if (totalReceived > 0)
        {
            po.Status = PurchaseOrderStatus.Partial_Received;
        }

        po.UpdatedAt = DateTime.UtcNow;

        _dbContext.InventoryReceipts.Add(receipt);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Created inventory receipt {ReceiptId} for purchase order {PoId}",
            receipt.Id, po.Id);

        return CreatedAtAction(nameof(GetReceipt), new { id = receipt.Id }, new InventoryReceiptResponse
        {
            Id = receipt.Id,
            PurchaseOrderId = receipt.PurchaseOrderId,
            ReceiptDate = receipt.ReceiptDate,
            ReceivedByUserId = receipt.ReceivedByUserId,
            ReceivedByUserName = user.Name,
            Notes = receipt.Notes,
            CreatedAt = receipt.CreatedAt,
            Items = receipt.Items.Select(i => new InventoryReceiptItemResponse
            {
                Id = i.Id,
                InventoryReceiptId = i.InventoryReceiptId,
                PurchaseOrderItemId = i.PurchaseOrderItemId,
                ReceivedQuantity = i.ReceivedQuantity,
                Condition = i.Condition.ToString(),
                Notes = i.Notes
            }).ToList()
        });
    }

    /// <summary>
    /// Get a single receipt
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<InventoryReceiptResponse>> GetReceipt(int id)
    {
        var receipt = await _dbContext.InventoryReceipts
            .Include(r => r.ReceivedByUser)
            .Include(r => r.Items)
                .ThenInclude(i => i.PurchaseOrderItem)
                    .ThenInclude(poi => poi!.SupplierProduct)
            .Include(r => r.PurchaseOrder)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receipt is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();
        if (user is null || ((UserType)user.UserTypeId != UserType.SuperAdmin && user.CompanyId != receipt.PurchaseOrder?.CompanyId))
        {
            return Forbid();
        }

        return Ok(new InventoryReceiptResponse
        {
            Id = receipt.Id,
            PurchaseOrderId = receipt.PurchaseOrderId,
            ReceiptDate = receipt.ReceiptDate,
            ReceivedByUserId = receipt.ReceivedByUserId,
            ReceivedByUserName = receipt.ReceivedByUser?.Name,
            Notes = receipt.Notes,
            CreatedAt = receipt.CreatedAt,
            Items = receipt.Items.Select(i => new InventoryReceiptItemResponse
            {
                Id = i.Id,
                InventoryReceiptId = i.InventoryReceiptId,
                PurchaseOrderItemId = i.PurchaseOrderItemId,
                ProductName = i.PurchaseOrderItem?.SupplierProduct?.ProductName ?? "",
                ReceivedQuantity = i.ReceivedQuantity,
                Condition = i.Condition.ToString(),
                Notes = i.Notes
            }).ToList()
        });
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _dbContext.Users.FindAsync(userId);
    }
}
