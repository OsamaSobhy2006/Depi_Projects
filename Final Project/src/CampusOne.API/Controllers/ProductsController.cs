
using System.Security.Claims;
using CampusOne.Application.DTOs.Sales;
using CampusOne.Domain.Entities.Sales;
using CampusOne.Infrastructure.Persistence;
using CampusOne.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly CloudinaryImageService _imageService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        ApplicationDbContext context,
        CloudinaryImageService imageService,
        ILogger<ProductsController> logger)
    {
        _context = context;
        _imageService = imageService;
        _logger = logger;
    }

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub");

    private bool IsAdmin =>
        User.IsInRole("Admin") ||
        User.Claims.Any(c =>
            (c.Type == ClaimTypes.Role ||
             c.Type == "role" ||
             c.Type == "roles")
            && c.Value == "Admin");

    private bool CanManageProduct(Product product)
    {
        return IsAdmin ||
               (!string.IsNullOrEmpty(CurrentUserId) &&
                product.UserId == CurrentUserId);
    }

    // GET: /Products
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var isAdmin = IsAdmin;

        var products = await _context.Products
            .AsNoTracking()
            .Where(p => isAdmin || (p.IsAvailable && p.Category.IsActive))
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.ImageUrl,
                p.CategoryId,
                CategoryName = p.Category.Name,
                p.StockQuantity,
                p.IsAvailable,
                p.CreatedAt,
                p.UserId,
                SellerName = _context.Users
                    .Where(u => u.Id == p.UserId)
                    .Select(u => (u.FirstName + " " + u.LastName).Trim())
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(products);
    }

    // GET: /Products/mine
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyProducts()
    {
        var userId = CurrentUserId;

        if (!IsAdmin && string.IsNullOrEmpty(userId))
            return Unauthorized();

        var products = await _context.Products
            .AsNoTracking()
            .Where(p => IsAdmin || p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.ImageUrl,
                p.CategoryId,
                CategoryName = p.Category.Name,
                p.StockQuantity,
                p.IsAvailable,
                p.CreatedAt,
                p.UserId,
                SellerName = _context.Users
                    .Where(u => u.Id == p.UserId)
                    .Select(u => (u.FirstName + " " + u.LastName).Trim())
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(products);
    }

    // GET: /Products/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var userId = CurrentUserId;
        var isAdmin = IsAdmin;

        var product = await _context.Products
            .AsNoTracking()
            .Where(p =>
                p.Id == id &&
                (isAdmin ||
                 p.UserId == userId ||
                 (p.IsAvailable && p.Category.IsActive)))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.ImageUrl,
                p.CategoryId,
                CategoryName = p.Category.Name,
                p.StockQuantity,
                p.IsAvailable,
                p.CreatedAt,
                p.UserId,
                SellerName = _context.Users
                    .Where(u => u.Id == p.UserId)
                    .Select(u => (u.FirstName + " " + u.LastName).Trim())
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (product == null)
            return NotFound("Product not found.");

        return Ok(product);
    }

    // GET: /Products/category/{categoryId}
    [HttpGet("category/{categoryId:guid}")]
    public async Task<IActionResult> GetProductsByCategory(Guid categoryId)
    {
        var isAdmin = IsAdmin;

        var products = await _context.Products
            .AsNoTracking()
            .Where(p =>
                p.CategoryId == categoryId &&
                (isAdmin || (p.IsAvailable && p.Category.IsActive)))
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.ImageUrl,
                p.CategoryId,
                CategoryName = p.Category.Name,
                p.StockQuantity,
                p.IsAvailable,
                p.CreatedAt,
                p.UserId,
                SellerName = _context.Users
                    .Where(u => u.Id == p.UserId)
                    .Select(u => (u.FirstName + " " + u.LastName).Trim())
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(products);
    }

    // POST: /Products/upload-image
    // Upload product image to Cloudinary.
    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(
    IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Please select an image.");

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest("Image size must not exceed 5 MB.");

        var allowedTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        if (!allowedTypes.Contains(
                file.ContentType.ToLowerInvariant()))
        {
            return BadRequest(
                "Only JPG, PNG, and WEBP images are allowed.");
        }

        try
        {
            var imageUrl = await _imageService.UploadImageAsync(file);

            return Ok(new
            {
                imageUrl,
                message = "Image uploaded successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product image upload failed.");

            return StatusCode(502, new
            {
                message = "Image upload failed. Please try again."
            });
        }
    }

    // POST: /Products
    [HttpPost]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductRequest request)
    {
        var userId = CurrentUserId;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User identity was not found.");

        var categoryExists = await _context.ProductCategories
            .AnyAsync(c => c.Id == request.CategoryId && c.IsActive);

        if (!categoryExists)
            return BadRequest("Active category not found.");

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            CategoryId = request.CategoryId,
            StockQuantity = request.StockQuantity,
            IsAvailable = true,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var result = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == product.Id)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.ImageUrl,
                p.CategoryId,
                CategoryName = p.Category.Name,
                p.StockQuantity,
                p.IsAvailable,
                p.CreatedAt,
                p.UserId,
                SellerName = _context.Users
                    .Where(u => u.Id == p.UserId)
                    .Select(u => (u.FirstName + " " + u.LastName).Trim())
                    .FirstOrDefault()
            })
            .FirstAsync();

        return CreatedAtAction(
            nameof(GetProduct),
            new { id = product.Id },
            result);
    }

    // PUT: /Products/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductRequest request)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound("Product not found.");

        if (!CanManageProduct(product))
            return Forbid();

        var categoryExists = await _context.ProductCategories
            .AnyAsync(c => c.Id == request.CategoryId && c.IsActive);

        if (!categoryExists)
            return BadRequest("Active category not found.");

        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl;
        product.CategoryId = request.CategoryId;
        product.StockQuantity = request.StockQuantity;
        product.IsAvailable = request.IsAvailable;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Product updated successfully.",
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.ImageUrl,
            product.CategoryId,
            product.StockQuantity,
            product.IsAvailable,
            product.UserId
        });
    }

    // DELETE: /Products/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound("Product not found.");

        if (!CanManageProduct(product))
            return Forbid();

        product.IsAvailable = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Product deactivated successfully."
        });
    }
}
