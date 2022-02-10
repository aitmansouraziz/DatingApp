﻿using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
       
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHubContext<PresenceHub> _presenceHub;
        private readonly PresenceTracker _presenceTracker;

        public MessageHub(IUnitOfWork unitOfWork,IMapper  mapper,
                          IHubContext<PresenceHub> presenceHub,
                            PresenceTracker presenceTracker)
        {
           
            this._unitOfWork = unitOfWork;
            this._mapper = mapper;
            this._presenceHub = presenceHub;
            this._presenceTracker = presenceTracker;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GetGroupName(Context.User.GetUsername(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group = await AddToGroup(groupName);
            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);
            var messages = await _unitOfWork.MessageRepository
                .GetMessageThread(Context.User.GetUsername(), otherUser);
            if (_unitOfWork.HasChange())
            {
                await _unitOfWork.Complete();
            }
            await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
           var group= await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
            await base.OnDisconnectedAsync(exception);
        }
        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
            var username = Context.User.GetUsername();
            if (createMessageDto.RecipientUsername.ToLower() == username) throw new HubException ("you cant send message to  yourself");
            var sender = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            var recipient = await _unitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);
            if (recipient is null) throw new HubException("not found user");

            var message = new Message()
            {
                Content = createMessageDto.Content,
                RecipientUsername = recipient.UserName,
                SenderUsername = sender.UserName,
                Sender = sender,
                Recipient = recipient
            };
            var groupName = GetGroupName(sender.UserName, recipient.UserName);
            var group = await _unitOfWork.MessageRepository.GetMessageGroup(groupName);

            if (group.Connections.Any(x=>x.Username==recipient.UserName))
            {
                message.DateRead = DateTime.UtcNow;
            }
            else
            {
                var connections = await _presenceTracker.GetConnectionsForUser(recipient.UserName);
                if (connections!=null)
                {
                    await _presenceHub.Clients.Clients(connections).SendAsync("NewMessageRecieved",
                        new { username = sender.UserName, knowAs = sender.KnownAs });
                }
            }
            _unitOfWork.MessageRepository.AddMessage(message);
            var messageDto = this._mapper.Map<MessageDto>(message);
            if (await _unitOfWork.Complete()) {
                
                await Clients.Group(groupName).SendAsync("NewMessage", messageDto);
            } 
           
        }

        private async Task<Group> AddToGroup(string groupName)
        {
            var group = await _unitOfWork.MessageRepository.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUsername());
            if (group==null)
            {
                group = new Group(groupName);
                _unitOfWork.MessageRepository.AddGroup(group);
            }
            group.Connections.Add(connection);
            if( await _unitOfWork.Complete()) return group;
            throw new HubException("Failed to join group");

        }

        private  async Task<Group> RemoveFromMessageGroup()
        {
            var group = await _unitOfWork.MessageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            _unitOfWork.MessageRepository.RemoveConnection(connection);
           if( await _unitOfWork.Complete()) return group;
            throw new HubException("Failed to remove from group");
        }
        private string GetGroupName(String caller,string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}"; 
        }
    }
}