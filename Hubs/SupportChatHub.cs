using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using store.Data;
using store.Dto;
using store.Models;
using System.Security.Claims;

namespace store.Hubs
{
    [Authorize]
    public class SupportChatHub(StoreDbContext context) : Hub
    {
        private readonly StoreDbContext _context = context;

        

        public override async Task OnConnectedAsync()
        {
            var role = Context.User?.FindFirstValue(ClaimTypes.Role);

            if(role == "Admin")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }

            await base.OnConnectedAsync();
        }

        public async Task SendMessageToAdmin(string message)
        {
            var senderIdString = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(senderIdString, out int senderId)) return;

            var sender = await _context.Users.FindAsync(senderId);

            var chatMessage = new ChatMessages
            {
                SenderId = senderId,
                Message = message
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            var dto = new ChatMessageDto
            {
                Id = chatMessage.Id,
                SenderId = senderId,
                SenderName = sender?.Name ?? "Unknown User",
                SenderProfilePic = sender?.ProfilePictureUrl,
                Message = message,
                Timestamp = chatMessage.TimeStamp
            };

            await Clients.Group("Admins").SendAsync("ReceiveMessage", dto);

            await Clients.Caller.SendAsync("ReceiveMessage", dto);
        }

        [Authorize(Roles = "Admin")] 
        public async Task SendMessageToCustomer(int customerId, string message)
        {
            var senderIdString = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(senderIdString, out int adminId)) return;

            var admin = await _context.Users.FindAsync(adminId);

            var chatMessage = new ChatMessages
            {
                SenderId = adminId,
                ReceiverId = customerId,
                Message = message
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            var dto = new ChatMessageDto
            {
                Id = chatMessage.Id,
                SenderId = adminId,
                SenderName = admin?.Name!,
                SenderProfilePic = admin?.ProfilePictureUrl,
                Message = message,
                Timestamp = chatMessage.TimeStamp
            };

            await Clients.User(customerId.ToString()).SendAsync("ReceiveMessage", dto);

            await Clients.Caller.SendAsync("ReceiveMessage", dto);
        }

    }
}
