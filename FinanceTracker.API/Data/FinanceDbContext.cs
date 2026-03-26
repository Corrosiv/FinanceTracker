using Microsoft.EntityFrameworkCore;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Data
{
    public class FinanceDbContext : DbContext
    {
        public FinanceDbContext(DbContextOptions<FinanceDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Budget> Budgets => Set<Budget>();
        public DbSet<Import> Imports => Set<Import>();
        public DbSet<RawImportRow> RawImportRows => Set<RawImportRow>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(u => u.Id);
                e.HasIndex(u => u.Email).IsUnique();
            });

            // Category
            modelBuilder.Entity<Category>(e =>
            {
                e.HasKey(c => c.Id);
                e.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
                e.HasOne(c => c.User)
                 .WithMany(u => u.Categories)
                 .HasForeignKey(c => c.UserId);
            });

            // Import
            modelBuilder.Entity<Import>(e =>
            {
                e.HasKey(i => i.Id);
                e.HasIndex(i => new { i.UserId, i.UploadDate });
                e.HasOne(i => i.User)
                 .WithMany(u => u.Imports)
                 .HasForeignKey(i => i.UserId);
                e.Property(i => i.Status)
                 .HasConversion<string>();
            });

            // Transaction
            modelBuilder.Entity<Transaction>(e =>
            {
                e.HasKey(t => t.Id);
                e.HasIndex(t => new { t.UserId, t.Date, t.Amount, t.NormalizedDescription }).IsUnique();
                e.HasIndex(t => new { t.UserId, t.CategoryId, t.Date });
                e.HasIndex(t => t.DeduplicationHash);
                e.Property(t => t.Amount).HasColumnType("decimal(12,2)");
                e.Property(t => t.Balance).HasColumnType("decimal(12,2)");
                e.HasOne(t => t.User)
                 .WithMany(u => u.Transactions)
                 .HasForeignKey(t => t.UserId);
                e.HasOne(t => t.Import)
                 .WithMany(i => i.Transactions)
                 .HasForeignKey(t => t.ImportId);
                e.HasOne(t => t.Category)
                 .WithMany(c => c.Transactions)
                 .HasForeignKey(t => t.CategoryId);
            });

            // RawImportRow
            modelBuilder.Entity<RawImportRow>(e =>
            {
                e.HasKey(r => r.Id);
                e.HasIndex(r => new { r.ImportId, r.RowNumber }).IsUnique();
                e.HasOne(r => r.Import)
                 .WithMany()
                 .HasForeignKey(r => r.ImportId);
                e.HasOne(r => r.Transaction)
                 .WithMany()
                 .HasForeignKey(r => r.TransactionId);
            });

            // Budget
            modelBuilder.Entity<Budget>(e =>
            {
                e.HasKey(b => b.Id);
                e.HasIndex(b => new { b.UserId, b.CategoryId, b.Period });
                e.Property(b => b.LimitAmount).HasColumnType("decimal(12,2)");
                e.Property(b => b.Period).HasConversion<string>();
                e.HasOne(b => b.User)
                 .WithMany(u => u.Budgets)
                 .HasForeignKey(b => b.UserId);
                e.HasOne(b => b.Category)
                 .WithMany(c => c.Budgets)
                 .HasForeignKey(b => b.CategoryId);
            });
        }
    }
}
