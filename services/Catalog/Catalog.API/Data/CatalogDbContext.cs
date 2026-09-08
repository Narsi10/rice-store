using Catalog.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Data;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.Brand).HasMaxLength(120);
            b.Property(p => p.Price).HasColumnType("decimal(18,2)");
            b.Property(p => p.CostPrice).HasColumnType("decimal(18,2)");
            b.Property(p => p.Description).HasMaxLength(1000);
        });
    }
}
