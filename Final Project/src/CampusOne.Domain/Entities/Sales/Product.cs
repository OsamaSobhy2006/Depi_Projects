
namespace CampusOne.Domain.Entities.Sales;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public Guid CategoryId { get; set; }

    public int StockQuantity { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Product owner (ASP.NET Identity User Id)
    public string? UserId { get; set; }

    public ProductCategory Category { get; set; } = null!;
}
