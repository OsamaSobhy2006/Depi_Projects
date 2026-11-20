using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CampusOne.Api.Hubs;

[Authorize]
public class StudyRoomHub : Hub
{
    private record PeerInfo(string UserId, string UserName);

    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PeerInfo>> ActiveRooms = new();

    private string GetUserId() =>
        Context.UserIdentifier
        ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Context.User?.FindFirstValue("sub")
        ?? Context.ConnectionId;

    private string GetUserName() =>
        Context.User?.FindFirstValue("name")
        ?? Context.User?.FindFirstValue(ClaimTypes.Name)
        ?? Context.User?.Identity?.Name
        ?? Context.User?.FindFirstValue(ClaimTypes.Email)
        ?? "Student";

    public async Task JoinRoom(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
            return;

        var cleanRoomCode = roomCode.Trim();
        var userId = GetUserId();
        var userName = GetUserName();
        var room = ActiveRooms.GetOrAdd(cleanRoomCode, _ => new ConcurrentDictionary<string, PeerInfo>());

        var existingPeers = room
            .Select(p => new
            {
                ConnectionId = p.Key,
                UserId = p.Value.UserId,
                UserName = p.Value.UserName
            })
            .ToList();

        room[Context.ConnectionId] = new PeerInfo(userId, userName);
        await Groups.AddToGroupAsync(Context.ConnectionId, cleanRoomCode);

        await Clients.Caller.SendAsync("ExistingPeers", existingPeers);
        await Clients.OthersInGroup(cleanRoomCode).SendAsync("UserJoined", new
        {
            ConnectionId = Context.ConnectionId,
            UserId = userId,
            UserName = userName
        });
    }

    public async Task SendOffer(string targetConnectionId, object offer)
    {
        await Clients.Client(targetConnectionId).SendAsync("ReceiveOffer", new
        {
            SenderConnectionId = Context.ConnectionId,
            SenderName = GetUserName(),
            Offer = offer
        });
    }

    public async Task SendAnswer(string targetConnectionId, object answer)
    {
        await Clients.Client(targetConnectionId).SendAsync("ReceiveAnswer", new
        {
            SenderConnectionId = Context.ConnectionId,
            Answer = answer
        });
    }

    public async Task SendIceCandidate(string targetConnectionId, object candidate)
    {
        await Clients.Client(targetConnectionId).SendAsync("ReceiveIceCandidate", new
        {
            SenderConnectionId = Context.ConnectionId,
            Candidate = candidate
        });
    }

    public async Task LeaveRoom(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
            return;

        await RemovePeerFromRoomAsync(roomCode.Trim(), Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var roomCode in ActiveRooms.Keys.ToList())
        {
            if (ActiveRooms.TryGetValue(roomCode, out var room) && room.ContainsKey(Context.ConnectionId))
            {
                await RemovePeerFromRoomAsync(roomCode, Context.ConnectionId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task RemovePeerFromRoomAsync(string roomCode, string connectionId)
    {
        if (ActiveRooms.TryGetValue(roomCode, out var room) && room.TryRemove(connectionId, out var peer))
        {
            await Groups.RemoveFromGroupAsync(connectionId, roomCode);
            await Clients.Group(roomCode).SendAsync("UserLeft", new
            {
                ConnectionId = connectionId,
                UserId = peer.UserId,
                UserName = peer.UserName
            });

            if (room.IsEmpty)
            {
                ActiveRooms.TryRemove(roomCode, out _);
            }
        }
    }
}