using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoicingSystem.Models
{
    public class InvoiceItem
    {
        [Key]
        public int InvoiceItemId { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [Required]
        [StringLength(255)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.00, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Range(0.00, 100.00, ErrorMessage = "Discount percentage must be between 0 and 100")]
        public decimal DiscountPercentage { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }

        [StringLength(50)]
        public string? Unit { get; set; } 

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("InvoiceId")]
        public virtual Invoice Invoice { get; set; } = null!;

    
        [NotMapped]
        public decimal LineTotalBeforeDiscount => Quantity * UnitPrice;

    
        public void CalculateLineTotal()
        {
            var totalBeforeDiscount = Quantity * UnitPrice;
            
            if (DiscountPercentage > 0)
            {
                DiscountAmount = totalBeforeDiscount * (DiscountPercentage / 100);
            }
            
            LineTotal = totalBeforeDiscount - DiscountAmount;
        }
    }
}