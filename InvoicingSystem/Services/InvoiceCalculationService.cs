using InvoicingSystem.Models;
using InvoicingSystem.DTOs;
using System.ComponentModel.DataAnnotations;

namespace InvoicingSystem.Services
{
    public interface IInvoiceCalculationService
    {
        decimal CalculateLineTotal(decimal quantity, decimal unitPrice, decimal discountPercentage, decimal discountAmount);
        decimal CalculateSubtotal(List<InvoiceItem> items);
        decimal CalculateTaxAmount(decimal subtotal, decimal taxRate);
        decimal CalculateGrandTotal(decimal subtotal, decimal taxAmount);
        List<string> ValidateInvoiceItem(CreateInvoiceItemDto item);
        List<string> ValidateInvoice(CreateInvoiceDto invoice);
    }

    public class InvoiceCalculationService : IInvoiceCalculationService
    {
        public decimal CalculateLineTotal(decimal quantity, decimal unitPrice, decimal discountPercentage, decimal discountAmount)
        {
            if (quantity <= 0) throw new ArgumentException("Quantity must be greater than 0");
            if (unitPrice < 0) throw new ArgumentException("Unit price cannot be negative");
            if (discountPercentage < 0 || discountPercentage > 100) 
                throw new ArgumentException("Discount percentage must be between 0 and 100");
            
            var lineSubtotal = quantity * unitPrice;
            
            // Apply percentage discount first
            var percentageDiscount = lineSubtotal * (discountPercentage / 100);
            
            // Apply absolute discount
            var totalDiscount = percentageDiscount + discountAmount;
            
            // Ensure discount doesn't exceed line subtotal
            if (totalDiscount > lineSubtotal)
                throw new ArgumentException("Total discount cannot exceed line subtotal");
            
            return lineSubtotal - totalDiscount;
        }

        public decimal CalculateSubtotal(List<InvoiceItem> items)
        {
            return items.Sum(item => item.LineTotal);
        }

        public decimal CalculateTaxAmount(decimal subtotal, decimal taxRate)
        {
            if (taxRate < 0 || taxRate > 100)
                throw new ArgumentException("Tax rate must be between 0 and 100");
            
            return subtotal * (taxRate / 100);
        }

        public decimal CalculateGrandTotal(decimal subtotal, decimal taxAmount)
        {
            return subtotal + taxAmount;
        }

        public List<string> ValidateInvoiceItem(CreateInvoiceItemDto item)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(item.ProductName))
                errors.Add("Product name is required");

            if (item.Quantity <= 0)
                errors.Add("Quantity must be greater than 0");

            if (item.UnitPrice < 0)
                errors.Add("Unit price cannot be negative");

            if (item.DiscountPercentage < 0 || item.DiscountPercentage > 100)
                errors.Add("Discount percentage must be between 0 and 100");

            if (item.DiscountAmount < 0)
                errors.Add("Discount amount cannot be negative");

            // Validate total discount doesn't exceed line value
            var lineSubtotal = item.Quantity * item.UnitPrice;
            var percentageDiscount = lineSubtotal * (item.DiscountPercentage / 100);
            var totalDiscount = percentageDiscount + item.DiscountAmount;

            if (totalDiscount > lineSubtotal)
                errors.Add("Total discount cannot exceed line subtotal");

            return errors;
        }

        public List<string> ValidateInvoice(CreateInvoiceDto invoice)
        {
            var errors = new List<string>();

            if (invoice.CustomerId <= 0)
                errors.Add("Valid Customer ID is required");

            if (invoice.TaxRate < 0 || invoice.TaxRate > 100)
                errors.Add("Tax rate must be between 0 and 100");

            if (invoice.InvoiceItems == null || !invoice.InvoiceItems.Any())
                errors.Add("At least one invoice item is required");

            if (invoice.InvoiceDate == default)
                errors.Add("Invoice date is required");

            if (invoice.DueDate.HasValue && invoice.DueDate.Value < invoice.InvoiceDate)
                errors.Add("Due date cannot be earlier than invoice date");

            // Validate each invoice item
            if (invoice.InvoiceItems != null)
            {
                for (int i = 0; i < invoice.InvoiceItems.Count; i++)
                {
                    var itemErrors = ValidateInvoiceItem(invoice.InvoiceItems[i]);
                    foreach (var error in itemErrors)
                    {
                        errors.Add($"Item {i + 1}: {error}");
                    }
                }
            }

            return errors;
        }
    }
}