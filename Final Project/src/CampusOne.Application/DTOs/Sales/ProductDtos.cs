
using System.ComponentModel.DataAnnotations;

namespace CampusOne.Application.DTOs.Sales;

public class CreateProductRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Url]
    public string? ImageUrl { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Range(0, 100000)]
    public int StockQuantity { get; set; }
}

public class UpdateProductRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Url]
    public string? ImageUrl { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Range(0, 100000)]
    public int StockQuantity { get; set; }

    public bool IsAvailable { get; set; }
}
