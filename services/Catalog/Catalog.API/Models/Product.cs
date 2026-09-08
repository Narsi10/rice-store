using BuildingBlocks.Domain;

namespace Catalog.API.Models;

/// <summary>A rice product sold in the store.</summary>
public class Product : Entity
{
    public string Name { get; set; } = default!;          // e.g. "India Gate Basmati"
    public string Description { get; set; } = default!;
    public RiceType RiceType { get; set; }                // Basmati, Brown, Jasmine...
    public string Brand { get; set; } = default!;
    public decimal Price { get; set; }                    // selling price for the given weight
    public decimal CostPrice { get; set; }                // buying/cost price (for profit tracking)
    public int Stock { get; set; }                        // units available
    public int WeightKg { get; set; }                     // pack size: 1, 5, 25...
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public enum RiceType
{
    HMT = 0,
    SonaMasoori = 1,
    Basmati = 2,
    SinglePolish = 3,
    JaiShreeRam = 4,
    Other = 99
}
