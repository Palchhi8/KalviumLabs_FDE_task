using InvoicingSystem.Data;
using InvoicingSystem.DTOs;
using InvoicingSystem.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text.Json;

namespace InvoicingSystem.Services
{
    public interface IStoredProcedureService
    {
        Task<int> AddInvoiceAsync(CreateInvoiceDto invoiceDto);
        Task<bool> EditInvoiceAsync(int invoiceId, UpdateInvoiceDto invoiceDto);
        Task<bool> VoidInvoiceAsync(int invoiceId);
        Task<(List<InvoiceDto> invoices, int totalCount)> SearchInvoiceAsync(
            int? customerId = null,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 10);
        Task<InvoiceTotalsDto> CalculateTotalsAsync(List<InvoiceItemCalculationDto> items, decimal taxRate);
    }

    public class StoredProcedureService : IStoredProcedureService
    {
        private readonly string _connectionString;
        private readonly ILogger<StoredProcedureService> _logger;

        public StoredProcedureService(IConfiguration configuration, ILogger<StoredProcedureService> logger)
        {
            _connectionString = configuration.GetConnectionString("SqlServerConnection") 
                ?? throw new InvalidOperationException("Connection string 'SqlServerConnection' not found.");
            _logger = logger;
        }

        private async Task<SqlConnection> GetConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            var retryCount = 3;
            var delay = TimeSpan.FromSeconds(1);

            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    await connection.OpenAsync();
                    return connection;
                }
                catch (SqlException ex) when (i < retryCount - 1)
                {
                    _logger.LogWarning($"SQL connection attempt {i + 1} failed: {ex.Message}. Retrying in {delay.TotalSeconds} seconds...");
                    await Task.Delay(delay);
                    delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2); // Exponential backoff
                }
            }

            // Final attempt without catch
            await connection.OpenAsync();
            return connection;
        }

        public async Task<int> AddInvoiceAsync(CreateInvoiceDto invoiceDto)
        {
            try
            {
                var itemsJson = JsonSerializer.Serialize(invoiceDto.InvoiceItems);
                _logger.LogInformation($"Adding invoice for customer {invoiceDto.CustomerId} with {invoiceDto.InvoiceItems.Count} items");

                using var connection = await GetConnectionAsync();
                using var command = new SqlCommand("sp_AddInvoice", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 60
                };

                command.Parameters.AddWithValue("@CustomerId", invoiceDto.CustomerId);
                command.Parameters.AddWithValue("@InvoiceDate", invoiceDto.InvoiceDate);
                command.Parameters.AddWithValue("@DueDate", invoiceDto.DueDate ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TaxRate", invoiceDto.TaxRate);
                command.Parameters.AddWithValue("@Notes", invoiceDto.Notes ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ItemsJson", itemsJson);

                var outputParam = new SqlParameter("@InvoiceId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(outputParam);

                await command.ExecuteNonQueryAsync();

                var invoiceId = (int)outputParam.Value;
                _logger.LogInformation($"Successfully created invoice with ID: {invoiceId}");
                return invoiceId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding invoice for customer {invoiceDto.CustomerId}");
                throw;
            }
        }

        public async Task<bool> EditInvoiceAsync(int invoiceId, UpdateInvoiceDto invoiceDto)
        {
            try
            {
                var itemsJson = JsonSerializer.Serialize(invoiceDto.InvoiceItems);
                _logger.LogInformation($"Editing invoice {invoiceId} for customer {invoiceDto.CustomerId}");

                using var connection = await GetConnectionAsync();
                using var command = new SqlCommand("sp_EditInvoice", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 60
                };

                command.Parameters.AddWithValue("@InvoiceId", invoiceId);
                command.Parameters.AddWithValue("@CustomerId", invoiceDto.CustomerId);
                command.Parameters.AddWithValue("@InvoiceDate", invoiceDto.InvoiceDate);
                command.Parameters.AddWithValue("@DueDate", invoiceDto.DueDate ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TaxRate", invoiceDto.TaxRate);
                command.Parameters.AddWithValue("@Notes", invoiceDto.Notes ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ItemsJson", itemsJson);

                var outputParam = new SqlParameter("@Success", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(outputParam);

                await command.ExecuteNonQueryAsync();

                var success = (bool)outputParam.Value;
                _logger.LogInformation($"Invoice {invoiceId} edit result: {success}");
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error editing invoice {invoiceId}");
                throw;
            }
        }

        public async Task<bool> VoidInvoiceAsync(int invoiceId)
        {
            try
            {
                _logger.LogInformation($"Voiding invoice {invoiceId}");

                using var connection = await GetConnectionAsync();
                using var command = new SqlCommand("sp_VoidInvoice", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 60
                };

                command.Parameters.AddWithValue("@InvoiceId", invoiceId);

                var outputParam = new SqlParameter("@Success", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(outputParam);

                await command.ExecuteNonQueryAsync();

                var success = (bool)outputParam.Value;
                _logger.LogInformation($"Invoice {invoiceId} void result: {success}");
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error voiding invoice {invoiceId}");
                throw;
            }
        }

        public async Task<(List<InvoiceDto> invoices, int totalCount)> SearchInvoiceAsync(
            int? customerId = null,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 10)
        {
            try
            {
                _logger.LogInformation($"Searching invoices - Customer: {customerId}, Status: {status}, Page: {pageNumber}");

                using var connection = await GetConnectionAsync();
                using var command = new SqlCommand("sp_SearchInvoice", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 60
                };

                command.Parameters.AddWithValue("@CustomerId", customerId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", status ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@FromDate", fromDate ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ToDate", toDate ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PageNumber", pageNumber);
                command.Parameters.AddWithValue("@PageSize", pageSize);

                var totalCountParam = new SqlParameter("@TotalCount", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(totalCountParam);

                var invoices = new List<InvoiceDto>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    invoices.Add(new InvoiceDto
                    {
                        InvoiceId = reader.GetInt32("InvoiceId"),
                        InvoiceNumber = reader.GetString("InvoiceNumber"),
                        CustomerId = reader.GetInt32("CustomerId"),
                        CustomerName = reader.GetString("CustomerName"),
                        InvoiceDate = reader.GetDateTime("InvoiceDate"),
                        DueDate = reader.IsDBNull("DueDate") ? null : reader.GetDateTime("DueDate"),
                        TotalAmount = reader.GetDecimal("TotalAmount"),
                        Status = reader.GetString("Status")
                    });
                }

                var totalCount = totalCountParam.Value == null || totalCountParam.Value == DBNull.Value ? 0 : (int)totalCountParam.Value;
                _logger.LogInformation($"Found {invoices.Count} invoices, total count: {totalCount}");
                return (invoices, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching invoices");
                throw;
            }
        }

        public async Task<InvoiceTotalsDto> CalculateTotalsAsync(List<InvoiceItemCalculationDto> items, decimal taxRate)
        {
            try
            {
                var itemsJson = JsonSerializer.Serialize(items);
                _logger.LogInformation($"Calculating totals for {items.Count} items with tax rate {taxRate}%");

                using var connection = await GetConnectionAsync();
                using var command = new SqlCommand("sp_CalculateTotals", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 60
                };

                command.Parameters.AddWithValue("@ItemsJson", itemsJson);
                command.Parameters.AddWithValue("@TaxRate", taxRate);

                var subtotalParam = new SqlParameter("@Subtotal", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Output,
                    Precision = 18,
                    Scale = 2
                };
                command.Parameters.Add(subtotalParam);

                var taxAmountParam = new SqlParameter("@TaxAmount", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Output,
                    Precision = 18,
                    Scale = 2
                };
                command.Parameters.Add(taxAmountParam);

                var totalParam = new SqlParameter("@Total", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Output,
                    Precision = 18,
                    Scale = 2
                };
                command.Parameters.Add(totalParam);

                await command.ExecuteNonQueryAsync();

                var result = new InvoiceTotalsDto
                {
                    Subtotal = (decimal)subtotalParam.Value,
                    TaxAmount = (decimal)taxAmountParam.Value,
                    Total = (decimal)totalParam.Value
                };

                _logger.LogInformation($"Calculated totals - Subtotal: {result.Subtotal:C}, Tax: {result.TaxAmount:C}, Total: {result.Total:C}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating totals");
                throw;
            }
        }
    }

    // Supporting DTOs for stored procedure operations
    public class InvoiceItemCalculationDto
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    public class InvoiceTotalsDto
    {
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
    }
}