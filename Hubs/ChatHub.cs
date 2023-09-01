﻿using Communicator.Data;
using Communicator.Models;
using Communicator.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System.Data.Entity;
using System.Drawing;

namespace Communicator.Hubs
{
    public class ChatHub : Hub
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ChatService _chatService;
        private readonly ApplicationDbContext _context;
        
        public ChatHub(UserManager<ApplicationUser> userManager, ChatService chatService, ApplicationDbContext applicationDbContext)
        {
            _userManager = userManager;
            _chatService = chatService;
            _context = applicationDbContext;
        }

        public override async Task OnConnectedAsync()
        {
            var user = await _userManager.GetUserAsync(Context.User);
            _chatService.AddUser(user.Id, Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            //await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Communicator");
            var user = _chatService.GetUserByConnectionId(Context.ConnectionId);
            _chatService.RemoveUserFromList(user);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string messageContent, string receiverId, string sendingTime)
        {
            await Clients.Caller.SendAsync("SendMessage", messageContent, sendingTime);
             
            if (receiverId != null )
            {
                string connId;
                ApplicationUser sender; 
                if(_userManager.GetUserId(Context.User) == receiverId)
                {
                    var recId = _context.Correspondences.FirstOrDefault(c => c.ReceiverId == receiverId).SenderId;
                    sender = _userManager.Users.FirstOrDefault(u => u.Id == recId);
                    connId = _chatService.GetConnectionId(recId);
                    
                }
                else
                {
                    sender = _userManager.Users.FirstOrDefault(u => u.Id == receiverId);
                    connId = _chatService.GetConnectionId(receiverId);
                }
                
                await Clients.Client(connId).SendAsync("ReceiveMessage", messageContent, sendingTime, sender.Name);
            }
        }
    }
}
