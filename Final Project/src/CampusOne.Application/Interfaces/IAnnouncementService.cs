using CampusOne.Application.DTOs;

namespace CampusOne.Application.Interfaces;

public interface IAnnouncementService
{
    Task<List<AnnouncementDto>> GetAllAsync(AnnouncementQuery query, CancellationToken ct = default);
    Task<AnnouncementDto?> GetByIdAsync(int id, bool includeInactive = false, CancellationToken ct = default);
    Task<AnnouncementDto> CreateAsync(CreateAnnouncementDto dto, string? createdByUserId, CancellationToken ct = default);
    Task<AnnouncementDto?> UpdateAsync(int id, UpdateAnnouncementDto dto, string userId, bool isAdmin, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, string userId, bool isAdmin, CancellationToken ct = default);
}