using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Data;

/// <summary>Stock level for a single catalog product.</summary>
public class StockItem : Entity
{
    public Guid ProductId { get; set; }
    public int QuantityAvailable { get; set; }
    public int QuantityReserved { get; set; }
}

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<StockItem> Stock => Set<StockItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockItem>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.ProductId).IsUnique();
        });
    }
}
