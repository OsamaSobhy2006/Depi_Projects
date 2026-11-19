
using CampusOne.Domain.Entities.UniversityServices;
using CampusOne.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Infrastructure.Persistence.Seeders;

public static class UniversityServicesSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Add missing categories only
        var categoryNames = new[]
        {
            "Academic",
            "Administrative",
            "Financial",
            "Forms & Requests",
            "Support"
        };

        foreach (var name in categoryNames)
        {
            if (!await context.ServiceCategories.AnyAsync(c => c.Name == name))
            {
                context.ServiceCategories.Add(new ServiceCategory
                {
                    Name = name,
                    Description = $"{name} university services",
                    IsActive = true
                });
            }
        }

        await context.SaveChangesAsync();

        var categories = await context.ServiceCategories
            .ToDictionaryAsync(c => c.Name, c => c.Id);

        var services = new[]
        {
            new { Category = "Academic", Name = "Academic Services",
                Description = "Access academic information and records.",
                Instructions = "Contact the academic department for assistance." },

            new { Category = "Academic", Name = "Registration & Add/Drop",
                Description = "Manage course registration and add or drop courses.",
                Instructions = "Provide your student ID and course details." },

            new { Category = "Administrative", Name = "Student Affairs",
                Description = "Student affairs and student-related services.",
                Instructions = "Provide your student information." },

            new { Category = "Administrative", Name = "Transportation",
                Description = "University buses, schedules and routes.",
                Instructions = "Specify your transportation route." },

            new { Category = "Administrative", Name = "Health Services",
                Description = "Information about university health services.",
                Instructions = "Contact the university health center." },

            new { Category = "Administrative", Name = "Housing & Accommodation",
                Description = "Dormitories, rooms and housing information.",
                Instructions = "Provide your accommodation requirements." },

            new { Category = "Financial", Name = "Financial Services",
                Description = "Tuition fees, payment methods and financial inquiries.",
                Instructions = "Include your student ID and inquiry details." },

            new { Category = "Forms & Requests", Name = "Request Documents",
                Description = "Request official documents, certificates and transcripts.",
                Instructions = "Specify the required document." },

            new { Category = "Forms & Requests", Name = "Forms & Applications",
                Description = "Submit forms and applications for university services.",
                Instructions = "Specify the form or application required." },

            new { Category = "Support", Name = "Technical Support",
                Description = "IT support and technical assistance.",
                Instructions = "Describe the technical issue." },

            new { Category = "Support", Name = "FAQs",
                Description = "Frequently asked questions and answers.",
                Instructions = "Submit your question if you need further assistance." },

            new { Category = "Support", Name = "Contact Us",
                Description = "Get in touch with university support teams.",
                Instructions = "Describe your inquiry and the department concerned." }
        };

        foreach (var item in services)
        {
            var categoryId = categories[item.Category];

            var exists = await context.ServiceItems.AnyAsync(s =>
                s.CategoryId == categoryId &&
                s.Name == item.Name);

            if (!exists)
            {
                context.ServiceItems.Add(new ServiceItem
                {
                    CategoryId = categoryId,
                    Name = item.Name,
                    Description = item.Description,
                    Instructions = item.Instructions,
                    IsActive = true
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
