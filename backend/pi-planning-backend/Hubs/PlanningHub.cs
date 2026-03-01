using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PiPlanningBackend.Data;
using PiPlanningBackend.DTOs.SignalR;

namespace PiPlanningBackend.Hubs
{
    public class PlanningHub(AppDbContext dbContext, ILogger<PlanningHub> logger) : Hub
    {
        private static readonly ConcurrentDictionary<string, ConnectionState> Connections = new();
        private static readonly string[] CursorPalette =
        [
            "#3B82F6",
            "#8B5CF6",
            "#14B8A6",
            "#F59E0B",
            "#EF4444",
            "#10B981",
            "#6366F1",
            "#EC4899"
        ];

        public async Task JoinBoard(int boardId, string userId, string userName)
        {
            if (boardId <= 0)
            {
                throw new HubException("Invalid board id.");
            }

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(userName))
            {
                throw new HubException("User id and user name are required.");
            }

            bool boardExists = await dbContext.Boards.AsNoTracking().AnyAsync(b => b.Id == boardId);
            if (!boardExists)
            {
                throw new HubException($"Board {boardId} not found.");
            }

            if (Connections.TryGetValue(Context.ConnectionId, out ConnectionState? existingConnection)
                && existingConnection.BoardId != boardId)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetBoardGroupName(existingConnection.BoardId));
            }

            ConnectionState connection = BuildConnectionState(boardId, userId, userName);
            Connections[Context.ConnectionId] = connection;

            string boardGroup = GetBoardGroupName(boardId);
            await Groups.AddToGroupAsync(Context.ConnectionId, boardGroup);

            List<UserPresenceDto> snapshot = [.. Connections.Values
                .Where(value => value.BoardId == boardId)
                .GroupBy(value => value.UserId)
                .Select(group => ToUserPresenceDto(group.First(), "joined"))];

            await Clients.Caller.SendAsync("PresenceSnapshot", snapshot);
            await Clients.OthersInGroup(boardGroup).SendAsync("UserJoinedBoard", ToUserPresenceDto(connection, "joined"));

            logger.LogInformation("Connection {ConnectionId} joined board {BoardId} as {UserId}", Context.ConnectionId, boardId, userId);
        }

        public async Task LeaveBoard(int boardId, string userId)
        {
            if (Connections.TryRemove(Context.ConnectionId, out ConnectionState? connection))
            {
                string boardGroup = GetBoardGroupName(connection.BoardId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, boardGroup);
                await Clients.OthersInGroup(boardGroup).SendAsync("UserLeftBoard", ToUserPresenceDto(connection, "leave"));

                logger.LogInformation("Connection {ConnectionId} left board {BoardId} as {UserId}", Context.ConnectionId, boardId, userId);
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetBoardGroupName(boardId));
        }

        public async Task UpdateCursorPosition(int boardId, string userId, int x, int y, long sequence)
        {
            if (!Connections.TryGetValue(Context.ConnectionId, out ConnectionState? connection))
            {
                return;
            }

            if (connection.BoardId != boardId || !string.Equals(connection.UserId, userId, StringComparison.Ordinal))
            {
                return;
            }

            CursorPresenceDto payload = new()
            {
                BoardId = boardId,
                UserId = connection.UserId,
                DisplayName = connection.DisplayName,
                Cursor = new CursorPositionDto
                {
                    X = x,
                    Y = y
                },
                CoordinateSpace = "board",
                Color = connection.Color,
                Avatar = connection.Avatar,
                Activity = "active",
                Sequence = sequence,
                TimestampUtc = DateTime.UtcNow
            };

            await Clients.OthersInGroup(GetBoardGroupName(boardId)).SendAsync("CursorPresenceUpdated", payload);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Connections.TryRemove(Context.ConnectionId, out ConnectionState? connection))
            {
                string boardGroup = GetBoardGroupName(connection.BoardId);
                await Clients.OthersInGroup(boardGroup).SendAsync("UserLeftBoard", ToUserPresenceDto(connection, "disconnect"));

                logger.LogInformation("Connection {ConnectionId} disconnected from board {BoardId} as {UserId}", Context.ConnectionId, connection.BoardId, connection.UserId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public static string GetBoardGroupName(int boardId)
        {
            return $"board:{boardId}";
        }

        /// <summary>
        /// Broadcast an event to all clients in a board group, optionally excluding the initiator.
        /// </summary>
        /// <param name="clients">The IHubClients from IHubContext</param>
        /// <param name="boardId">The board ID to broadcast to</param>
        /// <param name="eventName">The event name to send (e.g., "FeatureImported")</param>
        /// <param name="payload">The payload to send with the event</param>
        /// <param name="initiatorConnectionId">Optional connection ID of the initiator to exclude from broadcast</param>
        public static async Task BroadcastToBoardAsync(
            IHubClients clients,
            int boardId,
            string eventName,
            object payload,
            string? initiatorConnectionId = null)
        {
            string boardGroup = GetBoardGroupName(boardId);

            if (!string.IsNullOrEmpty(initiatorConnectionId))
            {
                // Exclude the initiator from the broadcast
                await clients.GroupExcept(boardGroup, initiatorConnectionId).SendAsync(eventName, payload);
            }
            else
            {
                // Broadcast to all clients in the group
                await clients.Group(boardGroup).SendAsync(eventName, payload);
            }
        }

        private static ConnectionState BuildConnectionState(int boardId, string userId, string userName)
        {
            string normalizedName = userName.Trim();
            return new ConnectionState
            {
                BoardId = boardId,
                UserId = userId,
                DisplayName = normalizedName,
                Color = ResolveColor(userId),
                Avatar = ResolveAvatar(normalizedName)
            };
        }

        private static UserPresenceDto ToUserPresenceDto(ConnectionState connection, string reason)
        {
            return new UserPresenceDto
            {
                BoardId = connection.BoardId,
                UserId = connection.UserId,
                DisplayName = connection.DisplayName,
                Color = connection.Color,
                Avatar = connection.Avatar,
                TimestampUtc = DateTime.UtcNow,
                Reason = reason
            };
        }

        private static string ResolveColor(string userId)
        {
            int hash = 0;
            foreach (char character in userId)
            {
                hash = (hash * 31) + character;
            }

            int index = Math.Abs(hash) % CursorPalette.Length;
            return CursorPalette[index];
        }

        private static string ResolveAvatar(string displayName)
        {
            char firstCharacter = displayName.Trim().FirstOrDefault();
            return char.IsLetterOrDigit(firstCharacter)
                ? char.ToUpperInvariant(firstCharacter).ToString()
                : "?";
        }

        private class ConnectionState
        {
            public int BoardId { get; set; }
            public string UserId { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Color { get; set; } = "#3B82F6";
            public string Avatar { get; set; } = "?";
        }
    }
}
