using System.Linq.Expressions;
using CampusOne.Application.DTOs;
using CampusOne.Application.Interfaces;
using CampusOne.Domain.Entities;
using CampusOne.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Services;

public class ClubService(ApplicationDbContext db) : IClubService
{
    private static Expression<Func<Club, ClubDto>> Projection(string? userId) => c => new ClubDto
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        Category = c.Category,
        LogoUrl = c.LogoUrl,
        MeetingInfo = c.MeetingInfo,
        MaxMembers = c.MaxMembers,
        MemberCount = c.Memberships.Count(),
        IsMember = userId != null && c.Memberships.Any(m => m.UserId == userId),
        IsFull = c.MaxMembers.HasValue && c.Memberships.Count() >= c.MaxMembers.Value
    };

    public async Task<List<ClubDto>> GetAllAsync(ClubQuery query, string? userId, CancellationToken ct = default)
    {
        var q = db.Clubs.AsNoTracking().AsQueryable();

        if (!query.IncludeInactive)
            q = q.Where(c => c.IsActive);

        if (query.Category is { } category)
            q = q.Where(c => c.Category == category);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(c => c.Name.Contains(s) || c.Description.Contains(s));
        }

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        return await q
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Projection(userId))
            .ToListAsync(ct);
    }

    public async Task<ClubDto?> GetByIdAsync(
        int id, string? userId, bool includeInactive = false, CancellationToken ct = default)
    {
        var q = db.Clubs.AsNoTracking().Where(c => c.Id == id);
        if (!includeInactive) q = q.Where(c => c.IsActive);
        return await q.Select(Projection(userId)).FirstOrDefaultAsync(ct);
    }

    public async Task<List<ClubDto>> GetMyClubsAsync(string userId, CancellationToken ct = default) =>
        await db.Clubs.AsNoTracking()
            .Where(c => c.IsActive && c.Memberships.Any(m => m.UserId == userId))
            .OrderBy(c => c.Name)
            .Select(Projection(userId))
            .ToListAsync(ct);

    public async Task<JoinClubResult> JoinAsync(int clubId, string userId, CancellationToken ct = default)
    {
        var club = await db.Clubs.AsNoTracking()
            .Where(c => c.Id == clubId && c.IsActive)
            .Select(c => new { c.MaxMembers })
            .FirstOrDefaultAsync(ct);
        if (club is null) return JoinClubResult.ClubNotFound;

        if (await db.ClubMemberships.AnyAsync(m => m.ClubId == clubId && m.UserId == userId, ct))
            return JoinClubResult.AlreadyMember;

        if (club.MaxMembers is { } max &&
            await db.ClubMemberships.CountAsync(m => m.ClubId == clubId, ct) >= max)
            return JoinClubResult.ClubFull;

        db.ClubMemberships.Add(new ClubMembership { ClubId = clubId, UserId = userId });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique index (ClubId, UserId): a concurrent request already added the membership.
            return JoinClubResult.AlreadyMember;
        }
        return JoinClubResult.Joined;
    }

    public async Task<bool> LeaveAsync(int clubId, string userId, CancellationToken ct = default)
    {
        var membership = await db.ClubMemberships
            .FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == userId, ct);
        if (membership is null) return false;

        db.ClubMemberships.Remove(membership);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> NameExistsAsync(string name, int? excludeClubId = null, CancellationToken ct = default)
    {
        var n = name.Trim();
        return db.Clubs.AnyAsync(c => c.Name == n && (excludeClubId == null || c.Id != excludeClubId), ct);
    }

    public async Task<ClubDto> CreateAsync(CreateClubDto dto, CancellationToken ct = default)
    {
        var entity = new Club
        {
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Category = dto.Category,
            LogoUrl = dto.LogoUrl,
            MeetingInfo = dto.MeetingInfo,
            MaxMembers = dto.MaxMembers
        };

        db.Clubs.Add(entity);
        await db.SaveChangesAsync(ct);
        return (await GetByIdAsync(entity.Id, null, includeInactive: true, ct))!;
    }

    public async Task<ClubDto?> UpdateAsync(int id, UpdateClubDto dto, CancellationToken ct = default)
    {
        var entity = await db.Clubs.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return null;

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description.Trim();
        entity.Category = dto.Category;
        entity.LogoUrl = dto.LogoUrl;
        entity.MeetingInfo = dto.MeetingInfo;
        entity.MaxMembers = dto.MaxMembers;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, null, includeInactive: true, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Clubs.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return false;

        db.Clubs.Remove(entity); // memberships are removed by cascade
        await db.SaveChangesAsync(ct);
        return true;
    }
}