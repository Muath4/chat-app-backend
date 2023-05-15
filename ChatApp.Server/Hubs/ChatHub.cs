using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Server.Hubs
{

    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _rooms = new ConcurrentDictionary<string, string>();

        public async Task<string> CreateRoom()
        {
            var roomId = Guid.NewGuid().ToString();
            _rooms[roomId] = roomId;
            return roomId;
        }

        public async Task JoinRoom(string roomId, string userName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("ReceiveSystemMessage", $"{userName} has joined the room.");

            GroupHandler.GroupConnections.AddOrUpdate(roomId,
            new HashSet<string> { Context.ConnectionId },
            (key, value) => { value.Add(Context.ConnectionId); return value; });

            await Clients.Group(roomId).SendAsync("UpdateUserCount", GroupHandler.GroupConnections[roomId].Count);
        }

        public async Task LeaveRoom(string roomId, string userName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("LeaveRoom", $"{userName} has left the room.");

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            GroupHandler.GroupConnections.AddOrUpdate(roomId,
                new HashSet<string>(),
                (key, value) => { value.Remove(Context.ConnectionId); return value; });


            await Clients.Group(roomId).SendAsync("UpdateUserCount", GroupHandler.GroupConnections[roomId].Count);

        }

        public async Task SendMessage(string roomId, string user, string message)
        {
            await Clients.Group(roomId).SendAsync("ReceiveMessage", user, message);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            foreach (var group in GroupHandler.GroupConnections)
            {
                if (group.Value.Contains(Context.ConnectionId))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, group.Key);
                    GroupHandler.GroupConnections.AddOrUpdate(group.Key,
                        new HashSet<string>(),
                        (key, value) => { value.Remove(Context.ConnectionId); return value; });

                    await Clients.Group(group.Key).SendAsync("UpdateUserCount", GroupHandler.GroupConnections[group.Key].Count);
                    break;
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}