using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController:BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IMessageRepository _messageRepository;

        public MessagesController(IUserRepository userRepository, IMapper mapper,IMessageRepository messageRepository)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            this._messageRepository = messageRepository;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
        {
            var username = User.GetUsername();
            if (createMessageDto.RecipientUsername.ToLower() == username) return BadRequest("you cant send message to  yourself");
            var sender = await _userRepository.GetUserByUsernameAsync(username);
            var recipient =await _userRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);
            if (recipient is null) return NotFound();
           
            var message = new Message()
            {
                Content = createMessageDto.Content,
                RecipientUsername = recipient.UserName,
                SenderUsername=sender.UserName,
                Sender=sender,
                Recipient=recipient
            };
            _messageRepository.AddMessage(message);
            var messageDto = this._mapper.Map<MessageDto>(message);
            if (await _messageRepository.SaveAllAsync()) return Ok(messageDto);
            return BadRequest("Failed to send message");
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>>GetMesssagesForUser([FromQuery]
            MessageParams messageParams)
        {
            
            messageParams.Username = User.GetUsername();
            var messages = await _messageRepository.GetMessagesForUser(messageParams);
            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages);
            return Ok(messages);
        }

        [HttpGet("thread/{username}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageThread(string username)
        {
            var currentUsername = User.GetUsername();

            var messages = await _messageRepository.GetMessageThread(currentUsername, username);
            return Ok(messages);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var username = User.GetUsername();
            var message = await _messageRepository.GetMessage(id);
            if (message.Sender.UserName != username && message.Recipient.UserName!=username) return Unauthorized();
            if (message.Sender.UserName == username) message.SenderDeleted = true;
            if (message.Recipient.UserName == username) message.RecipientDeleted = true;
            if (message.RecipientDeleted && message.SenderDeleted) _messageRepository.DeleteMessage(message);

            if (await _messageRepository.SaveAllAsync()) return Ok();
            return BadRequest("Failed to delete message");
        }
    }
}
