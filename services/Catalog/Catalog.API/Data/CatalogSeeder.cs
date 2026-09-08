using Catalog.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Data;

/// <summary>Seeds a starter rice catalog so the store isn't empty on first run.</summary>
public static class CatalogSeeder
{
    public static async Task SeedAsync(CatalogDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.Products.AnyAsync()) return;

        db.Products.AddRange(
            new Product { Name = "HMT Premium Rice", Description = "Popular fine-grain HMT rice, soft and non-sticky when cooked.", RiceType = RiceType.HMT, Brand = "Sri Lakshmi", Price = 620m, CostPrice = 500m, Stock = 120, WeightKg = 5 },
            new Product { Name = "Sona Masoori Premium", Description = "Lightweight, aromatic South Indian rice.", RiceType = RiceType.SonaMasoori, Brand = "Sri Lalitha", Price = 850m, CostPrice = 700m, Stock = 75, WeightKg = 10 },
            new Product { Name = "India Gate Classic Basmati", Description = "Long-grain aged basmati rice.", RiceType = RiceType.Basmati, Brand = "India Gate", Price = 650m, CostPrice = 520m, Stock = 100, WeightKg = 5 },
            new Product { Name = "Single Polish Rice", Description = "Lightly polished rice retaining more nutrients and flavor.", RiceType = RiceType.SinglePolish, Brand = "Annapurna", Price = 560m, CostPrice = 450m, Stock = 80, WeightKg = 5 },
            new Product { Name = "Jai Shree Ram Rice", Description = "Premium quality Jai Shree Ram brand rice for daily use.", RiceType = RiceType.JaiShreeRam, Brand = "Jai Shree Ram", Price = 900m, CostPrice = 740m, Stock = 60, WeightKg = 10 }
        );

        await db.SaveChangesAsync();
    }
}
