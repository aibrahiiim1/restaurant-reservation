using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace RestaurantReservation.Web.Hubs;

[Authorize(Roles = "SuperAdmin,RestaurantManager,BranchManager")]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinBranchGroup(int branchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"branch_{branchId}");
        _logger.LogInformation("User {UserId} joined branch group {BranchId}", Context.UserIdentifier, branchId);
    }

    public async Task LeaveBranchGroup(int branchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"branch_{branchId}");
        _logger.LogInformation("User {UserId} left branch group {BranchId}", Context.UserIdentifier, branchId);
    }

    public async Task JoinRestaurantGroup(int restaurantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"restaurant_{restaurantId}");
        _logger.LogInformation("User {UserId} joined restaurant group {RestaurantId}", Context.UserIdentifier, restaurantId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Admin user connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Admin user disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
