using System.Linq.Expressions;
using CampusOne.Application.DTOs;
using CampusOne.Application.Interfaces;
using CampusOne.Domain.Entities;
using CampusOne.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Services;

public class AnnouncementService(ApplicationDbContext db) : IAnnouncementService
{
    private static readonly Expression<Func<Announcement, AnnouncementDto>> ToDto = a => new AnnouncementDto
    {
        Id = a.Id,
        Title = a.Title,
        Content = a.Content,
        Category = a.Category,
        IsPinned = a.IsPinned,
        PublishedAtUtc = a.PublishedAtUtc,
        ExpiresAtUtc = a.ExpiresAtUtc
    };

    public async Task<List<AnnouncementDto>> GetAllAsync(AnnouncementQuery query, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var q = db.Announcements.AsNoTracking().AsQueryable();

        if (!query.IncludeInactive)
            q = q.Where(a => a.PublishedAtUtc <= now && (a.ExpiresAtUtc == null || a.ExpiresAtUtc > now));

        if (query.Category is { } category)
            q = q.Where(a => a.Category == category);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(a => a.Title.Contains(s) || a.Content.Contains(s));
        }

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        return await q
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PublishedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(ct);
    }

    public async Task<AnnouncementDto?> GetByIdAsync(int id, bool includeInactive = false, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var q = db.Announcements.AsNoTracking().Where(a => a.Id == id);

        if (!includeInactive)
            q = q.Where(a => a.PublishedAtUtc <= now && (a.ExpiresAtUtc == null || a.ExpiresAtUtc > now));

        return await q.Select(ToDto).FirstOrDefaultAsync(ct);
    }

    public async Task<AnnouncementDto> CreateAsync(
        CreateAnnouncementDto dto, string? createdByUserId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var entity = new Announcement
        {
            Title = dto.Title.Trim(),
            Content = dto.Content.Trim(),
            Category = dto.Category,
            IsPinned = dto.IsPinned,
            PublishedAtUtc = dto.PublishedAtUtc ?? now,
            ExpiresAtUtc = dto.ExpiresAtUtc,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = now
        };

        db.Announcements.Add(entity);
        await db.SaveChangesAsync(ct);
        return ToDto.Compile()(entity);
    }

    public async Task<AnnouncementDto?> UpdateAsync(
        int id, UpdateAnnouncementDto dto, string userId, bool isAdmin, CancellationToken ct = default)
    {
        var entity = await db.Announcements.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (entity is null) return null;

        if (!isAdmin && !string.Equals(entity.CreatedByUserId, userId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("You can only modify your own announcements.");

        entity.Title = dto.Title.Trim();
        entity.Content = dto.Content.Trim();
        entity.Category = dto.Category;
        entity.IsPinned = dto.IsPinned;
        entity.ExpiresAtUtc = dto.ExpiresAtUtc;
        if (dto.PublishedAtUtc is { } published) entity.PublishedAtUtc = published;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return ToDto.Compile()(entity);
    }

    public async Task<bool> DeleteAsync(
        int id, string userId, bool isAdmin, CancellationToken ct = default)
    {
        var entity = await db.Announcements.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (entity is null) return false;

        if (!isAdmin && !string.Equals(entity.CreatedByUserId, userId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("You can only delete your own announcements.");

        db.Announcements.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}