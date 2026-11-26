
using CampusOne.Domain.Entities.Sales;
using CampusOne.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Infrastructure.Persistence.Seeders;

public static class ProductCategorySeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var categories = new List<ProductCategory>
        {
            new()
            {
                Name = "Books & Notes",
                Description = "Textbooks, study notes and academic materials",
                Icon = "book-open",
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                Name = "Stationery",
                Description = "Pens, notebooks and stationery supplies",
                Icon = "pencil",
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                Name = "University Merch",
                Description = "University clothing and branded merchandise",
                Icon = "shirt",
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                Name = "Electronics",
                Description = "Electronic devices and accessories",
                Icon = "laptop",
                DisplayOrder = 4,
                IsActive = true
            },
            new()
            {
                Name = "Food & Drinks",
                Description = "Food, snacks and beverages",
                Icon = "coffee",
                DisplayOrder = 5,
                IsActive = true
            },
            new()
            {
                Name = "Other",
                Description = "Other products and miscellaneous items",
                Icon = "package",
                DisplayOrder = 6,
                IsActive = true
            }
        };

        foreach (var category in categories)
        {
            var exists = await context.ProductCategories
                .AnyAsync(c => c.Name == category.Name);

            if (!exists)
            {
                context.ProductCategories.Add(category);
            }
        }

        await context.SaveChangesAsync();
    }
}
