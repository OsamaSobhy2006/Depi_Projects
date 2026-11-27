using CampusOne.Application.DTOs.LostAndFound;
using CampusOne.Application.Interfaces.LostAndFound;
using CampusOne.Domain.Entities.LostAndFound;
using CampusOne.Domain.Enums;
using CampusOne.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusOne.Services.LostAndFound;

public class LostAndFoundService : ILostAndFoundService
{
    private const double MatchThreshold = 0.72;
    private readonly ApplicationDbContext _context;
    private readonly IImageComparisonService _imageComparisonService;

    public LostAndFoundService(
        ApplicationDbContext context,
        IImageComparisonService imageComparisonService)
    {
        _context = context;
        _imageComparisonService = imageComparisonService;
    }

    public async Task<CreateLostAndFoundResponse> CreateReportAsync(
        CreateLostAndFoundRequest request,
        CancellationToken cancellationToken = default)
    {
        var imageUrl = await SaveImageAsync(
            request.ImageStream,
            request.FileName,
            cancellationToken);

        var incidentDate = request.IncidentDate == default
            ? DateTime.UtcNow
            : request.IncidentDate;

        var item = new LostAndFoundItem
        {
            Id = Guid.NewGuid(),
            ReporterUserId = request.ReporterUserId,
            Type = request.Type,
            Title = request.Title.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            Description = request.Description.Trim(),
            Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
            IncidentDate = incidentDate,
            ImageUrl = imageUrl,
            Status = LostAndFoundStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        _context.LostAndFoundItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        var matches = await FindMatchingItemsInternalAsync(item, cancellationToken);

        if (matches.Count > 0)
        {
            item.Status = LostAndFoundStatus.MatchFound;

            var matchedIds = matches.Select(m => m.MatchedItemId).ToList();
            var matchedItems = await _context.LostAndFoundItems
                .Where(x => matchedIds.Contains(x.Id) && x.Status == LostAndFoundStatus.Open)
                .ToListAsync(cancellationToken);

            foreach (var matched in matchedItems)
            {
                matched.Status = LostAndFoundStatus.MatchFound;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        var guidance = BuildGuidanceMessage(item, matches);

        return new CreateLostAndFoundResponse
        {
            ItemId = item.Id,
            Status = item.Status,
            GuidanceMessage = guidance,
            PossibleMatches = matches
        };
    }

    public async Task<IReadOnlyList<LostAndFoundItem>> GetReportsAsync(
        LostAndFoundType? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LostAndFoundItems
            .AsNoTracking()
            .AsQueryable();

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MatchCandidateDto>> FindMatchesAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.LostAndFoundItems
            .FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken)
            ?? throw new KeyNotFoundException("Lost and found item not found.");

        return await FindMatchingItemsInternalAsync(item, cancellationToken);
    }

    public async Task<LostAndFoundClaim> SubmitClaimAsync(
        Guid lostItemId,
        Guid foundItemId,
        string claimantUserId,
        string? verificationNote,
        CancellationToken cancellationToken = default)
    {
        var lostItem = await _context.LostAndFoundItems
            .FirstOrDefaultAsync(x => x.Id == lostItemId, cancellationToken)
            ?? throw new KeyNotFoundException("Lost item not found.");

        var foundItem = await _context.LostAndFoundItems
            .FirstOrDefaultAsync(x => x.Id == foundItemId, cancellationToken)
            ?? throw new KeyNotFoundException("Found item not found.");

        var similarity = await _imageComparisonService.CompareImagesAsync(
            lostItem.ImageUrl,
            foundItem.ImageUrl,
            cancellationToken);

        var claim = new LostAndFoundClaim
        {
            Id = Guid.NewGuid(),
            LostItemId = lostItem.Id,
            FoundItemId = foundItem.Id,
            ClaimantUserId = claimantUserId,
            SimilarityScore = similarity,
            VerificationNote = string.IsNullOrWhiteSpace(verificationNote) ? null : verificationNote.Trim(),
            Status = ClaimStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.LostAndFoundClaims.Add(claim);

        lostItem.Status = LostAndFoundStatus.ClaimPending;
        foundItem.Status = LostAndFoundStatus.ClaimPending;

        await _context.SaveChangesAsync(cancellationToken);

        return claim;
    }

    public async Task ResolveItemAsync(
        Guid itemId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.LostAndFoundItems
            .FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken)
            ?? throw new KeyNotFoundException("Lost and found item not found.");

        var relatedClaims = await _context.LostAndFoundClaims
            .Where(c => c.LostItemId == itemId || c.FoundItemId == itemId)
            .ToListAsync(cancellationToken);

        var isParticipant = item.ReporterUserId == userId ||
            relatedClaims.Any(c => c.ClaimantUserId == userId);

        if (!isParticipant)
            throw new UnauthorizedAccessException("You are not allowed to resolve this item.");

        var idsToResolve = new HashSet<Guid> { item.Id };

        foreach (var claim in relatedClaims)
        {
            idsToResolve.Add(claim.LostItemId);
            idsToResolve.Add(claim.FoundItemId);

            if (claim.Status == ClaimStatus.Pending)
                claim.Status = ClaimStatus.Approved;
        }

        var oppositeType = item.Type == LostAndFoundType.Lost
            ? LostAndFoundType.Found
            : LostAndFoundType.Lost;

        var matchedPairs = await _context.LostAndFoundItems
            .Where(x => x.Id != item.Id
                && x.Type == oppositeType
                && x.Status != LostAndFoundStatus.Resolved)
            .ToListAsync(cancellationToken);

        foreach (var candidate in matchedPairs)
        {
            var score = await _imageComparisonService.CompareImagesAsync(
                item.ImageUrl,
                candidate.ImageUrl,
                cancellationToken);

            if (score >= MatchThreshold)
            {
                idsToResolve.Add(candidate.Id);
            }
        }

        var itemsToResolve = await _context.LostAndFoundItems
            .Where(x => idsToResolve.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var target in itemsToResolve)
        {
            target.Status = LostAndFoundStatus.Resolved;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteReportAsync(
        Guid itemId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.LostAndFoundItems
            .FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken)
            ?? throw new KeyNotFoundException("Lost and found item not found.");

        if (item.ReporterUserId != userId)
            throw new UnauthorizedAccessException("Only the report owner can delete this item.");

        var claims = await _context.LostAndFoundClaims
            .Where(c => c.LostItemId == itemId || c.FoundItemId == itemId)
            .ToListAsync(cancellationToken);

        if (claims.Count > 0)
            _context.LostAndFoundClaims.RemoveRange(claims);

        _context.LostAndFoundItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<MatchCandidateDto>> FindMatchingItemsInternalAsync(
        LostAndFoundItem sourceItem,
        CancellationToken cancellationToken)
    {
        var oppositeType = sourceItem.Type == LostAndFoundType.Lost
            ? LostAndFoundType.Found
            : LostAndFoundType.Lost;

        var candidates = await _context.LostAndFoundItems
            .AsNoTracking()
            .Where(x => x.Id != sourceItem.Id
                && x.Type == oppositeType
                && x.Status != LostAndFoundStatus.Resolved)
            .ToListAsync(cancellationToken);

        var results = new List<MatchCandidateDto>();

        foreach (var candidate in candidates)
        {
            var score = await _imageComparisonService.CompareImagesAsync(
                sourceItem.ImageUrl,
                candidate.ImageUrl,
                cancellationToken);

            if (score >= MatchThreshold)
            {
                var locationHint = string.IsNullOrWhiteSpace(candidate.Location)
                    ? "the campus lost-and-found desk"
                    : candidate.Location;

                results.Add(new MatchCandidateDto
                {
                    MatchedItemId = candidate.Id,
                    MatchedItemType = candidate.Type,
                    Title = candidate.Title,
                    Location = candidate.Location,
                    ImageUrl = candidate.ImageUrl,
                    SimilarityScore = Math.Round(score, 4),
                    GuidanceMessage =
                        $"Possible match ({Math.Round(score * 100)}%) near {locationHint}. Submit a claim to verify ownership and coordinate pickup/handover."
                });
            }
        }

        return results
            .OrderByDescending(r => r.SimilarityScore)
            .ToList();
    }

    private static string BuildGuidanceMessage(
        LostAndFoundItem item,
        IReadOnlyList<MatchCandidateDto> matches)
    {
        if (matches.Count > 0)
        {
            var top = matches[0];
            var loc = string.IsNullOrWhiteSpace(top.Location)
                ? "the campus lost-and-found desk"
                : top.Location;

            return item.Type == LostAndFoundType.Lost
                ? $"We found {matches.Count} potential visual match(es) for your lost item (top match: {Math.Round(top.SimilarityScore * 100)}%). Review the match and submit a claim to coordinate pickup near {loc}."
                : $"We found {matches.Count} potential visual match(es) for this found item (top match: {Math.Round(top.SimilarityScore * 100)}%). Keep it safe and coordinate handover near {loc}.";
        }

        return item.Type == LostAndFoundType.Lost
            ? "Your lost item report is open. We will keep comparing new found-item photos and notify you when a visual match appears."
            : "Your found item report is open. Please keep the item safe and bring it to the campus lost-and-found desk when a verified owner coordinates handover.";
    }

    private static async Task<string> SaveImageAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken)
    {
        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var apiWebRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CampusOne.API", "wwwroot"));

        var targetRoot = Directory.Exists(webRoot)
            ? webRoot
            : Directory.Exists(apiWebRoot)
                ? apiWebRoot
                : webRoot;

        var uploadsFolder = Path.Combine(targetRoot, "uploads", "lost-and-found");
        Directory.CreateDirectory(uploadsFolder);

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".jpg";

        var savedName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, savedName);

        if (imageStream.CanSeek)
            imageStream.Position = 0;

        await using var stream = new FileStream(filePath, FileMode.Create);
        await imageStream.CopyToAsync(stream, cancellationToken);

        return $"/uploads/lost-and-found/{savedName}";
    }
}