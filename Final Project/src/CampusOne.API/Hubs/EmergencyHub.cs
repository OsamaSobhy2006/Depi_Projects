using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CampusOne.API.Hubs;

[Authorize]
public class EmergencyHub : Hub
{
    public const string RespondersGroupName = "EmergencyResponders";

    private static bool IsResponder(System.Security.Claims.ClaimsPrincipal? user)
    {
        return user is not null &&
               (user.IsInRole("Admin") || user.IsInRole("Staff"));
    }

    public override async Task OnConnectedAsync()
    {
        if (IsResponder(Context.User))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, RespondersGroupName);
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinResponderGroup()
    {
        if (IsResponder(Context.User))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, RespondersGroupName);
        }
    }

    public async Task JoinIncidentGroup(Guid incidentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetIncidentGroupName(incidentId));
    }

    public async Task LeaveIncidentGroup(Guid incidentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetIncidentGroupName(incidentId));
    }

    public static string GetIncidentGroupName(Guid incidentId) => $"EmergencyIncident_{incidentId}";
}