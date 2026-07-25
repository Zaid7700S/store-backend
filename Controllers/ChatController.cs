using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using store.Data;
using store.Dto;
using store.Models;
using System.Security.Claims;

namespace store.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController(StoreDbContext context) : ControllerBase
    {
        private readonly StoreDbContext _context = context;

        [HttpGet("history")]

        public async Task<ActionResult<List<ChatMessageDto>>> GetChatHistory()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var query = from message in _context.ChatMessages
                        join user in _context.Users on message.SenderId equals user.Id
                        select new ChatMessageDto
                        {
                            Id = message.Id,
                            SenderId = message.SenderId,
                            SenderName = user.Name!,
                            SenderProfilePic = user.ProfilePictureUrl,
                            ReceiverId = message.ReceiverId,
                            Message = message.Message,
                            Timestamp = message.TimeStamp
                        };

            if(role == "Admin")
            {
                var adminHistory = await query.OrderBy(x => x.Timestamp).ToListAsync();
                return Ok(adminHistory);
            }

            else
            {
                var customerHistory = await query
                                        .Where(x => x.SenderId == userId || x.ReceiverId == userId)
                                        .OrderBy(x => x.Timestamp)
                                        .ToListAsync();
                return Ok(customerHistory);
            }
        }
    }
}
