
namespace CampusOne.Domain.Entities;

public class ClubMembership
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;

    /// <summary>Identity user id (string). Change the type if your users use Guid/int.</summary>
    public string UserId { get; set; } = string.Empty;

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}