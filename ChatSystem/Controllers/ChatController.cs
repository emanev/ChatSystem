using ChatSystem.Data;
using ChatSystem.Hubs;
using ChatSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace ChatSystem.Controllers
{    
    public class ChatController : Controller
    {
        private readonly DataContext db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(DataContext dataContext, UserManager<IdentityUser> userManager, IHubContext<ChatHub> hubContext)
        {
            db = dataContext;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        [HttpPost]        
        [Produces("application/json")]
        public async Task<IActionResult> SendMessage([FromBody] MessageViewModel model)
        {
            var to = model.To;
            var text = model.Text;
            if (!User.Identity.IsAuthenticated)
            {
                return StatusCode(500);
            }

            var user = await _userManager.GetUserAsync(HttpContext.User);
            var recipient = await db.Users.SingleOrDefaultAsync(x => x.UserName == to);

            Message message = new Message()
            {
                FromUserId = user.Id,
                ToUserId = recipient?.Id,
                Text = text,
                Timestamp = DateTime.Now
            };        
          
            await db.AddAsync(message);
            await db.SaveChangesAsync();

            string connectionId = ChatHub.UsernameConnectionId[recipient.UserName];
           
            await _hubContext.Clients.Client(connectionId).SendAsync("RecieveMessage", message.Text, message.Timestamp.ToShortTimeString());
           
            return Json(true);            
        }
    }
}
