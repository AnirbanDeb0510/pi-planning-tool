using Microsoft.AspNetCore.SignalR;

namespace PiPlanningBackend.Hubs
{
    public class PlanningHub : Hub
    {
        public async Task JoinBoard(string boardId) => await Groups.AddToGroupAsync(Context.ConnectionId, boardId);

        public async Task LeaveBoard(string boardId) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, boardId);

        public async Task BroadcastBoardUpdate(string boardId, object update)
        {
            await Clients.OthersInGroup(boardId).SendAsync("ReceiveBoardUpdate", update);
        }

        public async Task SendCursor(string boardId, string user, double x, double y)
        {
            await Clients.OthersInGroup(boardId).SendAsync("ReceiveCursor", new { user, x, y });
        }
    }
}
