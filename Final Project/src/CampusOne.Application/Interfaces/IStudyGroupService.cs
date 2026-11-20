using CampusOne.Application.DTOs.Study;

namespace CampusOne.Application.Interfaces;

public interface IStudyGroupService
{
    Task<List<StudyGroupDto>> GetGroupsAsync(string? query = null, string? courseCode = null, bool onlyLive = false);
    Task<StudyGroupDto> CreateGroupAsync(string userId, CreateStudyGroupRequest request);
    Task<bool> JoinGroupAsync(Guid groupId, string userId);
    Task<StudySessionDto> StartSessionAsync(Guid groupId, string userId, StartStudySessionRequest request);
    Task<bool> EndSessionAsync(Guid sessionId, string userId);
}