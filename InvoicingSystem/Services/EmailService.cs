using InvoicingSystem.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace InvoicingSystem.Services
{
    public interface IEmailService
    {
        Task<bool> SendInvoiceEmailAsync(Invoice invoice);
        Task<bool> SendTestEmailAsync(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendInvoiceEmailAsync(Invoice invoice)
        {
            try
            {
                var subject = $"Invoice #{invoice.InvoiceNumber} - {invoice.Customer.FullName}";
                var htmlBody = GenerateInvoiceEmailHtml(invoice);

                // Check if email is enabled in configuration
                var emailEnabled = _configuration.GetValue<bool>("EmailSettings:EnableRealEmails", false);
                
                if (!emailEnabled)
                {
                    // Simulation mode (for development/testing)
                    _logger.LogInformation("SIMULATION: Email would be sent to {Email} for Invoice {InvoiceNumber}", 
                        invoice.Customer.Email, invoice.InvoiceNumber);
                    await Task.Delay(500); // Simulate delay
                    _logger.LogInformation("SIMULATION: Email successfully sent to {Email} for Invoice {InvoiceNumber}", 
                        invoice.Customer.Email, invoice.InvoiceNumber);
                    return true;
                }

                // Real email sending using MailKit
                _logger.LogInformation("Sending real email to {Email} for Invoice {InvoiceNumber}", 
                    invoice.Customer.Email, invoice.InvoiceNumber);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _configuration["EmailSettings:FromName"] ?? "Invoicing System",
                    _configuration["EmailSettings:FromEmail"] ?? "noreply@invoicingsystem.com"
                ));
                message.To.Add(new MailboxAddress(invoice.Customer.FullName, invoice.Customer.Email));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody,
                    TextBody = $"Please find your invoice #{invoice.InvoiceNumber} attached. Total Amount: ${invoice.TotalAmount:F2}"
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                
                // Connect to SMTP server
                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = _configuration.GetValue<int>("EmailSettings:SmtpPort", 587);
                var enableSsl = _configuration.GetValue<bool>("EmailSettings:EnableSsl", true);
                
                await client.ConnectAsync(smtpServer, smtpPort, enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                // Authenticate if credentials are provided
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];
                
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    await client.AuthenticateAsync(username, password);
                }

                // Send the email
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Real email successfully sent to {Email} for Invoice {InvoiceNumber}", 
                    invoice.Customer.Email, invoice.InvoiceNumber);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invoice email for Invoice {InvoiceNumber} to {Email}", 
                    invoice.InvoiceNumber, invoice.Customer.Email);
                return false;
            }
        }

        public async Task<bool> SendTestEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Check if email is enabled in configuration
                var emailEnabled = _configuration.GetValue<bool>("EmailSettings:EnableRealEmails", false);
                
                if (!emailEnabled)
                {
                    // Simulation mode
                    _logger.LogInformation("SIMULATION: Test email would be sent to {Email}", toEmail);
                    await Task.Delay(300);
                    _logger.LogInformation("SIMULATION: Test email successfully sent to {Email}", toEmail);
                    return true;
                }

                // Real email sending
                _logger.LogInformation("Sending real test email to {Email}", toEmail);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _configuration["EmailSettings:FromName"] ?? "Invoicing System",
                    _configuration["EmailSettings:FromEmail"] ?? "noreply@invoicingsystem.com"
                ));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $"<html><body><p>{body}</p><p>This is a test email from the Invoicing System.</p></body></html>",
                    TextBody = body
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                
                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = _configuration.GetValue<int>("EmailSettings:SmtpPort", 587);
                var enableSsl = _configuration.GetValue<bool>("EmailSettings:EnableSsl", true);
                
                await client.ConnectAsync(smtpServer, smtpPort, enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];
                
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    await client.AuthenticateAsync(username, password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Real test email successfully sent to {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email to {Email}", toEmail);
                return false;
            }
        }

        private string GenerateInvoiceEmailHtml(Invoice invoice)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Invoice #{invoice.InvoiceNumber}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; color: #333; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .invoice-details {{ margin: 20px 0; }}
        .customer-info {{ background-color: #f9f9f9; padding: 15px; margin: 10px 0; }}
        .items-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        .items-table th, .items-table td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
        .items-table th {{ background-color: #f2f2f2; font-weight: bold; }}
        .totals {{ text-align: right; margin: 20px 0; }}
        .total-row {{ font-weight: bold; font-size: 18px; }}
        .footer {{ margin-top: 30px; text-align: center; color: #666; font-size: 12px; }}
        .addresses {{ display: flex; justify-content: space-between; margin: 20px 0; }}
        .address-section {{ width: 48%; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>INVOICE</h1>
        <h2>#{invoice.InvoiceNumber}</h2>
    </div>

    <div class='invoice-details'>
        <p><strong>Invoice Date:</strong> {invoice.InvoiceDate:MMMM dd, yyyy}</p>
        {(invoice.DueDate.HasValue ? $"<p><strong>Due Date:</strong> {invoice.DueDate.Value:MMMM dd, yyyy}</p>" : "")}
        <p><strong>Status:</strong> {invoice.Status}</p>
    </div>

    <div class='customer-info'>
        <h3>Bill To:</h3>
        <p><strong>{invoice.Customer.FullName}</strong></p>
        <p>{invoice.Customer.Email}</p>
        {(!string.IsNullOrEmpty(invoice.Customer.Phone) ? $"<p>{invoice.Customer.Phone}</p>" : "")}";

            if (!string.IsNullOrEmpty(invoice.BillingAddress))
            {
                html += $@"
        <p>{invoice.BillingAddress}</p>
        <p>{invoice.BillingCity}, {invoice.BillingState} {invoice.BillingZipCode}</p>
        {(!string.IsNullOrEmpty(invoice.BillingCountry) ? $"<p>{invoice.BillingCountry}</p>" : "")}";
            }

            html += @"
    </div>";

            if (!string.IsNullOrEmpty(invoice.ShippingAddress) && 
                invoice.ShippingAddress != invoice.BillingAddress)
            {
                html += $@"
    <div class='customer-info'>
        <h3>Ship To:</h3>
        <p>{invoice.ShippingAddress}</p>
        <p>{invoice.ShippingCity}, {invoice.ShippingState} {invoice.ShippingZipCode}</p>
        {(!string.IsNullOrEmpty(invoice.ShippingCountry) ? $"<p>{invoice.ShippingCountry}</p>" : "")}
    </div>";
            }

            html += @"
    <table class='items-table'>
        <thead>
            <tr>
                <th>Item</th>
                <th>Description</th>
                <th>Qty</th>
                <th>Unit Price</th>
                <th>Discount</th>
                <th>Total</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var item in invoice.InvoiceItems)
            {
                html += $@"
            <tr>
                <td>{item.ProductName}</td>
                <td>{item.Description ?? ""}</td>
                <td>{item.Quantity} {item.Unit ?? ""}</td>
                <td>${item.UnitPrice:F2}</td>
                <td>{(item.DiscountPercentage > 0 ? $"{item.DiscountPercentage}% (${item.DiscountAmount:F2})" : "-")}</td>
                <td>${item.LineTotal:F2}</td>
            </tr>";
            }

            html += $@"
        </tbody>
    </table>

    <div class='totals'>
        <p><strong>Subtotal: ${invoice.Subtotal:F2}</strong></p>";

            if (invoice.TaxRate > 0)
            {
                html += $@"
        <p>Tax ({invoice.TaxRate}%): ${invoice.TaxAmount:F2}</p>";
            }

            html += $@"
        <p class='total-row'>Total Amount: ${invoice.TotalAmount:F2}</p>
    </div>";

            if (!string.IsNullOrEmpty(invoice.Notes))
            {
                html += $@"
    <div class='customer-info'>
        <h3>Notes:</h3>
        <p>{invoice.Notes}</p>
    </div>";
            }

            html += @"
    <div class='footer'>
        <p>Thank you for your business!</p>
        <p>This is an automated email from the Invoicing System.</p>
    </div>
</body>
</html>";

            return html;
        }
    }
}