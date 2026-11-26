
using CampusOne.Application.DTOs.Sales;
using CampusOne.Domain.Entities.Sales;
using CampusOne.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Categories
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.ProductCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.Icon,
                c.DisplayOrder,
                c.IsActive
            })
            .ToListAsync();

        return Ok(categories);
    }

    // GET: /Categories/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategory(Guid id)
    {
        var category = await _context.ProductCategories
            .AsNoTracking()
            .Where(c => c.Id == id && c.IsActive)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.Icon,
                c.DisplayOrder,
                c.IsActive
            })
            .FirstOrDefaultAsync();

        if (category == null)
            return NotFound("Category not found.");

        return Ok(category);
    }

    // POST: /Categories
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory(
        CreateCategoryRequest request)
    {
        var name = request.Name.Trim();

        var exists = await _context.ProductCategories
            .AnyAsync(c => c.Name == name);

        if (exists)
            return BadRequest("Category name already exists.");

        var category = new ProductCategory
        {
            Name = name,
            Description = request.Description?.Trim(),
            Icon = request.Icon?.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsActive = true
        };

        _context.ProductCategories.Add(category);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = category.Id },
            new
            {
                category.Id,
                category.Name,
                category.Description,
                category.Icon,
                category.DisplayOrder,
                category.IsActive
            });
    }

    // PUT: /Categories/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        UpdateCategoryRequest request)
    {
        var category = await _context.ProductCategories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            return NotFound("Category not found.");

        var name = request.Name.Trim();

        var exists = await _context.ProductCategories
            .AnyAsync(c => c.Id != id && c.Name == name);

        if (exists)
            return BadRequest("Another category has the same name.");

        category.Name = name;
        category.Description = request.Description?.Trim();
        category.Icon = request.Icon?.Trim();
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Category updated successfully.",
            category.Id,
            category.Name,
            category.Description,
            category.Icon,
            category.DisplayOrder,
            category.IsActive
        });
    }

    // DELETE: /Categories/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var category = await _context.ProductCategories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            return NotFound("Category not found.");

        // Soft delete: preserve products linked to this category.
        category.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Category deactivated successfully."
        });
    }
}
