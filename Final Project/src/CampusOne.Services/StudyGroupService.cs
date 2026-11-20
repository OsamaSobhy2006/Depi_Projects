using CampusOne.Application.DTOs.Study;
using CampusOne.Application.Interfaces;
using CampusOne.Domain.Entities;
using CampusOne.Domain.Enums;
using CampusOne.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Application.Services;

public class StudyGroupService : IStudyGroupService
{
    private readonly ApplicationDbContext _dbContext;
    public StudyGroupService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<StudyGroupDto>> GetGroupsAsync(string? query = null, string? courseCode = null, bool onlyLive = false)
    {
        var groupsSet = _dbContext.Set<StudyGroup>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(courseCode))
        {
            var code = courseCode.Trim().ToUpperInvariant();
            groupsSet = groupsSet.Where(g => g.CourseCode != null && g.CourseCode.ToUpper() == code);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var search = query.Trim().ToLowerInvariant();
            groupsSet = groupsSet.Where(g =>
                g.Title.ToLower().Contains(search) ||
                (g.Description != null && g.Description.ToLower().Contains(search)) ||
                (g.CourseCode != null && g.CourseCode.ToLower().Contains(search)));
        }

        if (onlyLive)
            groupsSet = groupsSet.Where(g => g.Sessions.Any(s => s.IsLive));

        return await groupsSet
            .OrderByDescending(g => g.Sessions.Any(s => s.IsLive))
            .ThenByDescending(g => g.CreatedAt)
            .Select(g => new StudyGroupDto
            {
                Id = g.Id,
                Title = g.Title,
                CourseCode = g.CourseCode,
                Description = g.Description,
                MembersCount = g.Members.Count,
                HasActiveSession = g.Sessions.Any(s => s.IsLive),
                ActiveRoomCode = g.Sessions
                    .Where(s => s.IsLive)
                    .OrderByDescending(s => s.StartedAt)
                    .Select(s => s.RoomCode)
                    .FirstOrDefault(),
                CreatedAt = g.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<StudyGroupDto> CreateGroupAsync(string userId, CreateStudyGroupRequest request)
    {
        var group = new StudyGroup
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            CourseCode = string.IsNullOrWhiteSpace(request.CourseCode) ? null : request.CourseCode.Trim().ToUpperInvariant(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        group.Members.Add(new StudyGroupMember
        {
            StudyGroupId = group.Id,
            UserId = userId,
            Role = StudyGroupRole.Admin,
            JoinedAt = DateTime.UtcNow
        });

        _dbContext.Set<StudyGroup>().Add(group);
        await _dbContext.SaveChangesAsync();

        return new StudyGroupDto
        {
            Id = group.Id,
            Title = group.Title,
            CourseCode = group.CourseCode,
            Description = group.Description,
            MembersCount = 1,
            HasActiveSession = false,
            ActiveRoomCode = null,
            CreatedAt = group.CreatedAt
        };
    }

    public async Task<bool> JoinGroupAsync(Guid groupId, string userId)
    {
        var groupExists = await _dbContext.Set<StudyGroup>().AnyAsync(g => g.Id == groupId);
        if (!groupExists)
            return false;

        var alreadyMember = await _dbContext.Set<StudyGroupMember>()
            .AnyAsync(m => m.StudyGroupId == groupId && m.UserId == userId);

        if (alreadyMember)
            return true;

        _dbContext.Set<StudyGroupMember>().Add(new StudyGroupMember
        {
            StudyGroupId = groupId,
            UserId = userId,
            Role = StudyGroupRole.Member,
            JoinedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<StudySessionDto> StartSessionAsync(Guid groupId, string userId, StartStudySessionRequest request)
    {
        var existingLive = await _dbContext.Set<StudySession>()
            .FirstOrDefaultAsync(s => s.StudyGroupId == groupId && s.IsLive);

        if (existingLive is not null)
        {
            return new StudySessionDto
            {
                Id = existingLive.Id,
                StudyGroupId = existingLive.StudyGroupId,
                Topic = existingLive.Topic,
                RoomCode = existingLive.RoomCode,
                IsLive = existingLive.IsLive,
                StartedAt = existingLive.StartedAt,
                EndedAt = existingLive.EndedAt
            };
        }

        var session = new StudySession
        {
            Id = Guid.NewGuid(),
            StudyGroupId = groupId,
            Topic = string.IsNullOrWhiteSpace(request.Topic) ? "Live Study Session" : request.Topic.Trim(),
            RoomCode = $"room-{Guid.NewGuid():N}"[..16],
            IsLive = true,
            StartedByUserId = userId,
            StartedAt = DateTime.UtcNow
        };

        _dbContext.Set<StudySession>().Add(session);
        await _dbContext.SaveChangesAsync();

        return new StudySessionDto
        {
            Id = session.Id,
            StudyGroupId = session.StudyGroupId,
            Topic = session.Topic,
            RoomCode = session.RoomCode,
            IsLive = session.IsLive,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt
        };
    }

    public async Task<bool> EndSessionAsync(Guid sessionId, string userId)
    {
        var session = await _dbContext.Set<StudySession>()
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null)
            return false;

        session.IsLive = false;
        session.EndedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}