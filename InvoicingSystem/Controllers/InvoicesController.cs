using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvoicingSystem.Data;
using InvoicingSystem.Models;
using InvoicingSystem.DTOs;
using InvoicingSystem.Services;
using System.ComponentModel.DataAnnotations;

namespace InvoicingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<InvoicesController> _logger;
        private readonly IStoredProcedureService _storedProcedureService;
        private readonly IInvoiceCalculationService _calculationService;

        public InvoicesController(
            ApplicationDbContext context, 
            IEmailService emailService, 
            ILogger<InvoicesController> logger,
            IStoredProcedureService storedProcedureService,
            IInvoiceCalculationService calculationService)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _storedProcedureService = storedProcedureService;
            _calculationService = calculationService;
        }

        
        [HttpPost]
        public async Task<ActionResult<ApiResponse<InvoiceResponseDto>>> CreateInvoice([FromBody] CreateInvoiceDto createInvoiceDto)
        {
            try
            {
                // Validate input using calculation service
                var validationErrors = _calculationService.ValidateInvoice(createInvoiceDto);
                if (validationErrors.Any())
                {
                    return BadRequest(new ApiResponse<InvoiceResponseDto>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        Errors = validationErrors
                    });
                }

                // Check if customer exists
                var customer = await _context.Customers.FindAsync(createInvoiceDto.CustomerId);
                if (customer == null)
                {
                    return BadRequest(new ApiResponse<InvoiceResponseDto>
                    {
                        Success = false,
                        Message = "Customer not found"
                    });
                }

                // Use stored procedure to create invoice
                var invoiceId = await _storedProcedureService.AddInvoiceAsync(createInvoiceDto);

                // Retrieve the created invoice
                var createdInvoice = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.InvoiceItems)
                    .FirstAsync(i => i.InvoiceId == invoiceId);

                // Send email
                var emailSent = await _emailService.SendInvoiceEmailAsync(createdInvoice);
                if (emailSent)
                {
                    createdInvoice.EmailSent = true;
                    createdInvoice.EmailSentDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                var response = MapToResponseDto(createdInvoice);

                _logger.LogInformation("Invoice {InvoiceNumber} created successfully for customer {CustomerId}", 
                    createdInvoice.InvoiceNumber, createInvoiceDto.CustomerId);

                return Ok(new ApiResponse<InvoiceResponseDto>
                {
                    Success = true,
                    Message = "Invoice created successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice for customer {CustomerId}", createInvoiceDto.CustomerId);
                return StatusCode(500, new ApiResponse<InvoiceResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the invoice"
                });
            }
        }

       
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<InvoiceResponseDto>>> UpdateInvoice(int id, [FromBody] UpdateInvoiceDto updateInvoiceDto)
        {
            try
            {
                // Use stored procedure to update invoice
                var success = await _storedProcedureService.EditInvoiceAsync(id, updateInvoiceDto);

                if (!success)
                {
                    return BadRequest(new ApiResponse<InvoiceResponseDto>
                    {
                        Success = false,
                        Message = "Invoice not found or cannot be updated (possibly voided)"
                    });
                }

                // Retrieve the updated invoice
                var updatedInvoice = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.InvoiceItems)
                    .FirstAsync(i => i.InvoiceId == id);

                var response = MapToResponseDto(updatedInvoice);

                _logger.LogInformation("Invoice {InvoiceNumber} updated successfully", updatedInvoice.InvoiceNumber);

                return Ok(new ApiResponse<InvoiceResponseDto>
                {
                    Success = true,
                    Message = "Invoice updated successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice {InvoiceId}", id);
                return StatusCode(500, new ApiResponse<InvoiceResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the invoice"
                });
            }
        }

       
        [HttpPut("{id}/void")]
        public async Task<ActionResult<ApiResponse<InvoiceResponseDto>>> VoidInvoice(int id)
        {
            try
            {
                // Use stored procedure to void invoice
                var success = await _storedProcedureService.VoidInvoiceAsync(id);

                if (!success)
                {
                    return BadRequest(new ApiResponse<InvoiceResponseDto>
                    {
                        Success = false,
                        Message = "Invoice not found or already voided"
                    });
                }

                // Retrieve the voided invoice
                var voidedInvoice = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.InvoiceItems)
                    .FirstAsync(i => i.InvoiceId == id);

                var response = MapToResponseDto(voidedInvoice);

                _logger.LogInformation("Invoice {InvoiceNumber} voided successfully", voidedInvoice.InvoiceNumber);

                return Ok(new ApiResponse<InvoiceResponseDto>
                {
                    Success = true,
                    Message = "Invoice voided successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voiding invoice {InvoiceId}", id);
                return StatusCode(500, new ApiResponse<InvoiceResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while voiding the invoice"
                });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<PagedResult<InvoiceResponseDto>>>> SearchInvoices([FromQuery] InvoiceSearchDto searchDto)
        {
            try
            {
                // Use stored procedure for search
                var (invoiceDtos, totalCount) = await _storedProcedureService.SearchInvoiceAsync(
                    searchDto.CustomerId,
                    searchDto.Status,
                    searchDto.FromDate,
                    searchDto.ToDate,
                    searchDto.PageNumber,
                    searchDto.PageSize);

                var totalPages = (int)Math.Ceiling((double)totalCount / searchDto.PageSize);

                // Convert DTOs to response DTOs - get full invoice data efficiently
                var responseData = new List<InvoiceResponseDto>();
                
                if (invoiceDtos.Any())
                {
                    var invoiceIds = invoiceDtos.Select(i => i.InvoiceId).ToList();
                    var fullInvoices = await _context.Invoices
                        .Include(i => i.Customer)
                        .Include(i => i.InvoiceItems)
                        .Where(i => invoiceIds.Contains(i.InvoiceId))
                        .ToListAsync();
                    
                    // Maintain order from stored procedure results
                    foreach (var invoiceDto in invoiceDtos)
                    {
                        var fullInvoice = fullInvoices.First(i => i.InvoiceId == invoiceDto.InvoiceId);
                        responseData.Add(MapToResponseDto(fullInvoice));
                    }
                }

                var pagedResult = new PagedResult<InvoiceResponseDto>
                {
                    Data = responseData,
                    TotalRecords = totalCount,
                    PageNumber = searchDto.PageNumber,
                    PageSize = searchDto.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = searchDto.PageNumber < totalPages,
                    HasPreviousPage = searchDto.PageNumber > 1
                };

                return Ok(new ApiResponse<PagedResult<InvoiceResponseDto>>
                {
                    Success = true,
                    Message = "Invoices retrieved successfully",
                    Data = pagedResult
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching invoices");
                return StatusCode(500, new ApiResponse<PagedResult<InvoiceResponseDto>>
                {
                    Success = false,
                    Message = "An error occurred while searching invoices"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<InvoiceResponseDto>>> GetInvoice(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.InvoiceId == id);

                if (invoice == null)
                {
                    return NotFound(new ApiResponse<InvoiceResponseDto>
                    {
                        Success = false,
                        Message = "Invoice not found"
                    });
                }

                var response = MapToResponseDto(invoice);

                return Ok(new ApiResponse<InvoiceResponseDto>
                {
                    Success = true,
                    Message = "Invoice retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice {InvoiceId}", id);
                return StatusCode(500, new ApiResponse<InvoiceResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the invoice"
                });
            }
        }

      
        [HttpPost("{id}/resend-email")]
        public async Task<ActionResult<ApiResponse<object>>> ResendInvoiceEmail(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.InvoiceId == id);

                if (invoice == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invoice not found"
                    });
                }

                var emailSent = await _emailService.SendInvoiceEmailAsync(invoice);
                
                if (emailSent)
                {
                    invoice.EmailSent = true;
                    invoice.EmailSentDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Invoice email sent successfully"
                    });
                }
                else
                {
                    return StatusCode(500, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to send invoice email"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending invoice email for {InvoiceId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while sending the invoice email"
                });
            }
        }

        private async Task<string> GenerateUniqueInvoiceNumberAsync()
        {
            var date = DateTime.UtcNow;
            var baseNumber = $"INV-{date.Year}{date.Month:D2}{date.Day:D2}";
            
            var lastInvoice = await _context.Invoices
                .Where(i => i.InvoiceNumber.StartsWith(baseNumber))
                .OrderByDescending(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastInvoice != null)
            {
                var lastSequence = lastInvoice.InvoiceNumber.Split('-').LastOrDefault();
                if (int.TryParse(lastSequence, out int lastSeq))
                {
                    sequence = lastSeq + 1;
                }
            }

            return $"{baseNumber}-{sequence:D4}";
        }

        private static InvoiceResponseDto MapToResponseDto(Invoice invoice)
        {
            return new InvoiceResponseDto
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                CustomerId = invoice.CustomerId,
                CustomerName = invoice.Customer.FullName,
                CustomerEmail = invoice.Customer.Email,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Subtotal = invoice.Subtotal,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount,
                TaxRate = invoice.TaxRate,
                Status = invoice.Status,
                Notes = invoice.Notes,
                CreatedDate = invoice.CreatedDate,
                UpdatedDate = invoice.UpdatedDate,
                EmailSent = invoice.EmailSent,
                EmailSentDate = invoice.EmailSentDate,
                BillingAddress = invoice.BillingAddress,
                BillingCity = invoice.BillingCity,
                BillingState = invoice.BillingState,
                BillingZipCode = invoice.BillingZipCode,
                BillingCountry = invoice.BillingCountry,
                ShippingAddress = invoice.ShippingAddress,
                ShippingCity = invoice.ShippingCity,
                ShippingState = invoice.ShippingState,
                ShippingZipCode = invoice.ShippingZipCode,
                ShippingCountry = invoice.ShippingCountry,
                Items = invoice.InvoiceItems.Select(item => new InvoiceItemResponseDto
                {
                    InvoiceItemId = item.InvoiceItemId,
                    ProductName = item.ProductName,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountPercentage = item.DiscountPercentage,
                    DiscountAmount = item.DiscountAmount,
                    LineTotal = item.LineTotal,
                    Unit = item.Unit
                }).ToList()
            };
        }

        // Calculate totals endpoint
        [HttpPost("calculate-totals")]
        public async Task<ActionResult<ApiResponse<InvoiceTotalsDto>>> CalculateTotals([FromBody] CalculateTotalsRequest request)
        {
            try
            {
                if (request.Items == null || !request.Items.Any())
                {
                    return BadRequest(new ApiResponse<InvoiceTotalsDto>
                    {
                        Success = false,
                        Message = "At least one item is required"
                    });
                }

                // Validate items using calculation service
                var validationErrors = new List<string>();
                foreach (var item in request.Items)
                {
                    var itemDto = new CreateInvoiceItemDto
                    {
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        DiscountPercentage = item.DiscountPercentage,
                        DiscountAmount = item.DiscountAmount
                    };
                    var itemErrors = _calculationService.ValidateInvoiceItem(itemDto);
                    validationErrors.AddRange(itemErrors);
                }

                if (validationErrors.Any())
                {
                    return BadRequest(new ApiResponse<InvoiceTotalsDto>
                    {
                        Success = false,
                        Message = "Invalid item data",
                        Errors = validationErrors
                    });
                }

                // Use stored procedure to calculate totals
                var totals = await _storedProcedureService.CalculateTotalsAsync(request.Items, request.TaxRate);

                return Ok(new ApiResponse<InvoiceTotalsDto>
                {
                    Success = true,
                    Message = "Totals calculated successfully",
                    Data = totals
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating totals");
                return StatusCode(500, new ApiResponse<InvoiceTotalsDto>
                {
                    Success = false,
                    Message = "An error occurred while calculating totals"
                });
            }
        }

        // Test email endpoint
        [HttpPost("test-email")]
        public async Task<ActionResult<ApiResponse<object>>> TestEmail([FromBody] TestEmailRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Email address is required"
                    });
                }

                var subject = request.Subject ?? "Test Email from Invoicing System";
                var body = request.Body ?? "This is a test email to verify the email functionality is working correctly.";

                var emailSent = await _emailService.SendTestEmailAsync(request.Email, subject, body);

                return Ok(new ApiResponse<object>
                {
                    Success = emailSent,
                    Message = emailSent ? "Test email sent successfully" : "Failed to send test email",
                    Data = new { 
                        Email = request.Email, 
                        Subject = subject,
                        EmailMode = emailSent ? "Email sent" : "Email failed"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email to {Email}", request.Email);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error while sending test email"
                });
            }
        }
    }

    public class TestEmailRequest
    {
        public string Email { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }

    public class CalculateTotalsRequest
    {
        public List<InvoiceItemCalculationDto> Items { get; set; } = new();
        public decimal TaxRate { get; set; }
    }
}