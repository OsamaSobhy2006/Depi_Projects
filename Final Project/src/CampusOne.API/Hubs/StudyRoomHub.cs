using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CampusOne.Api.Hubs;

[Authorize]
public class StudyRoomHub : Hub
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> ActiveRooms = new();

    private string GetUserId() =>
        Context.UserIdentifier
        ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Context.User?.FindFirstValue("sub")
        ?? Context.ConnectionId;

    public async Task JoinRoom(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
            return;

        var cleanRoomCode = roomCode.Trim();
        var userId = GetUserId();
        var room = ActiveRooms.GetOrAdd(cleanRoomCode, _ => new ConcurrentDictionary<string, string>());

        var existingPeers = room
            .Select(p => new { ConnectionId = p.Key, UserId = p.Value })
            .ToList();

        room[Context.ConnectionId] = userId;
        await Groups.AddToGroupAsync(Context.ConnectionId, cleanRoomCode);

        await Clients.Caller.SendAsync("ExistingPeers", existingPeers);
        await Clients.OthersInGroup(cleanRoomCode).SendAsync("UserJoined", new
        {
            ConnectionId = Context.ConnectionId,
            UserId = userId
        });
    }

    public async Task SendOffer(string targetConnectionId, object offer)
    {
        await Clients.Client(targetConnectionId).SendAsync("ReceiveOffer", new
        {
            SenderConnectionId = Context.ConnectionId,
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
        if (ActiveRooms.TryGetValue(roomCode, out var room) && room.TryRemove(connectionId, out var userId))
        {
            await Groups.RemoveFromGroupAsync(connectionId, roomCode);
            await Clients.Group(roomCode).SendAsync("UserLeft", new
            {
                ConnectionId = connectionId,
                UserId = userId
            });

            if (room.IsEmpty)
            {
                ActiveRooms.TryRemove(roomCode, out _);
            }
        }
    }
}