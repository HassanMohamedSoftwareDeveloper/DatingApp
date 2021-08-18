using System;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly IHubContext<PresenceHub> presenceHub;
        private readonly PresenceTracker presenceTracker;

        public MessageHub(IUnitOfWork unitOfWork, IMapper mapper,
        IHubContext<PresenceHub> presenceHub, PresenceTracker presenceTracker)
        {
            this.presenceHub = presenceHub;
            this.presenceTracker = presenceTracker;
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GetGroupName(Context.User.GetUserName(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group = await AddToGroup(groupName);
            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);


            var messages = await unitOfWork.MessageRepository.GetMessageThread(Context.User.GetUserName(), otherUser);
            if (unitOfWork.HasChanges()) await unitOfWork.Complete();
            await Clients.Caller.SendAsync("ReceiveMessageThread", messages);

        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var group = await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(CreateMessageDto createMessageDto)
        {

            var username = Context.User.GetUserName();
            if (username == createMessageDto.ReciptientUsername.ToLower()) throw new HubException("You cann't send messages to yourself");
            var sender = await unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            var reciptient = await unitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDto.ReciptientUsername);
            if (reciptient == null) throw new HubException("Not found user");

            var message = new Message
            {
                Sender = sender,
                Recipient = reciptient,
                SenderUsername = sender.UserName,
                RecipientUsername = reciptient.UserName,
                Content = createMessageDto.Content
            };

            var groupName = GetGroupName(sender.UserName, reciptient.UserName);
            var group = await unitOfWork.MessageRepository.GetMessageGroup(groupName);
            if (group.Connections.Any(x => x.Username == reciptient.UserName))
            {
                message.DateRead = DateTime.UtcNow;
            }
            else
            {
                var connections = await presenceTracker.GetConnectionsForUser(reciptient.UserName);
                if (connections != null)
                {
                    await presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived",
                    new
                    {
                        username = sender.UserName,
                        knownAs = sender.KnownAs
                    });
                }
            }

            unitOfWork.MessageRepository.AddMessage(message);
            if (await unitOfWork.Complete())
            {
                await Clients.Group(groupName).SendAsync("NewMessage", mapper.Map<MessageDto>(message));
            }
        }
        private async Task<Group> AddToGroup(string groupName)
        {
            var group = await unitOfWork.MessageRepository.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUserName());
            if (group == null)
            {
                group = new Group(groupName);
                unitOfWork.MessageRepository.AddGroup(group);
            }
            group.Connections.Add(connection);
            if (await unitOfWork.Complete()) return group;
            throw new HubException("Failed to join group");
        }
        private async Task<Group> RemoveFromMessageGroup()
        {
            var group = await unitOfWork.MessageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            unitOfWork.MessageRepository.RemoveConnection(connection);
            if (await unitOfWork.Complete()) return group;
            throw new HubException("Failed to remove from group");
        }
        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}