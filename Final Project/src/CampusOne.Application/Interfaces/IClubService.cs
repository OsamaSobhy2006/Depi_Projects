using CampusOne.Application.DTOs;

namespace CampusOne.Application.Interfaces;

public interface IClubService
{
    Task<List<ClubDto>> GetAllAsync(ClubQuery query, string? userId, CancellationToken ct = default);
    Task<ClubDto?> GetByIdAsync(int id, string? userId, bool includeInactive = false, CancellationToken ct = default);
    Task<List<ClubDto>> GetMyClubsAsync(string userId, CancellationToken ct = default);

    Task<JoinClubResult> JoinAsync(int clubId, string userId, CancellationToken ct = default);
    Task<bool> LeaveAsync(int clubId, string userId, CancellationToken ct = default);

    Task<bool> NameExistsAsync(string name, int? excludeClubId = null, CancellationToken ct = default);
    Task<ClubDto> CreateAsync(CreateClubDto dto, CancellationToken ct = default);
    Task<ClubDto?> UpdateAsync(int id, UpdateClubDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}