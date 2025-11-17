using Microsoft.EntityFrameworkCore;
using InvoicingSystem.Models;

namespace InvoicingSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity relationships and constraints

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerId);
                entity.Property(e => e.CustomerId).UseIdentityColumn();
                
                entity.HasIndex(e => e.Email).IsUnique();
                
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.State).IsRequired().HasMaxLength(100);
                
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
            });

          
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.InvoiceId);
                entity.Property(e => e.InvoiceId).UseIdentityColumn();
                
                entity.HasIndex(e => e.InvoiceNumber).IsUnique();
                
                entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Active");
                
                entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxRate).HasColumnType("decimal(5,2)").HasDefaultValue(0.00m);
                
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.InvoiceDate).HasDefaultValueSql("GETUTCDATE()");
                
             
                entity.HasOne(e => e.Customer)
                      .WithMany(c => c.Invoices)
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            
            modelBuilder.Entity<InvoiceItem>(entity =>
            {
                entity.HasKey(e => e.InvoiceItemId);
                entity.Property(e => e.InvoiceItemId).UseIdentityColumn();
                
                entity.Property(e => e.ProductName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountPercentage).HasColumnType("decimal(5,2)").HasDefaultValue(0.00m);
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0.00m);
                entity.Property(e => e.LineTotal).HasColumnType("decimal(18,2)");
                
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
                
               
                entity.HasOne(e => e.Invoice)
                      .WithMany(i => i.InvoiceItems)
                      .HasForeignKey(e => e.InvoiceId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            
            // Check constraints for data integrity using modern EF Core approach
            modelBuilder.Entity<Invoice>()
                .ToTable(t => t.HasCheckConstraint("CK_Invoice_Subtotal", "[Subtotal] >= 0"))
                .ToTable(t => t.HasCheckConstraint("CK_Invoice_TaxAmount", "[TaxAmount] >= 0"))
                .ToTable(t => t.HasCheckConstraint("CK_Invoice_TotalAmount", "[TotalAmount] >= 0"))
                .ToTable(t => t.HasCheckConstraint("CK_Invoice_TaxRate", "[TaxRate] >= 0 AND [TaxRate] <= 100"))
                .ToTable(t => t.HasCheckConstraint("CK_Invoice_Status", "[Status] IN ('Active', 'Void', 'Paid')"));

            modelBuilder.Entity<InvoiceItem>()
                .ToTable(t => t.HasCheckConstraint("CK_InvoiceItem_Quantity", "[Quantity] > 0"))
                .ToTable(t => t.HasCheckConstraint("CK_InvoiceItem_UnitPrice", "[UnitPrice] >= 0"))
                .ToTable(t => t.HasCheckConstraint("CK_InvoiceItem_DiscountPercentage", "[DiscountPercentage] >= 0 AND [DiscountPercentage] <= 100"));
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Customer || e.Entity is Invoice)
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is Customer customer)
                {
                    customer.UpdatedDate = DateTime.UtcNow;
                }
                else if (entry.Entity is Invoice invoice)
                {
                    invoice.UpdatedDate = DateTime.UtcNow;
                }
            }
        }
    }
}