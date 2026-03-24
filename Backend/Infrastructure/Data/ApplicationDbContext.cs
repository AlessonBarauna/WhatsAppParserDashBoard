using Microsoft.EntityFrameworkCore;
using WhatsAppParser.Domain.Entities;

namespace WhatsAppParser.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<RawMessage> RawMessages { get; set; } = null!;
    public DbSet<PriceHistory> PriceHistories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.NormalizedName)
            .IsUnique(false);

        modelBuilder.Entity<PriceHistory>()
            .Property(p => p.Price)
            .HasColumnType("decimal(18,2)");
    }
}
