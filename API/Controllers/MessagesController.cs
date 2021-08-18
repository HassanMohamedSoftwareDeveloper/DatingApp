using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController : BaseApiController
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;

        public MessagesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
        {
            var username = User.GetUserName();
            if (username == createMessageDto.ReciptientUsername.ToLower()) return BadRequest("You cann't send messages to yourself");
            var sender = await unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            var reciptient = await unitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDto.ReciptientUsername);
            if (reciptient == null) return NotFound();
            var message = new Message
            {
                Sender = sender,
                Recipient = reciptient,
                SenderUsername = sender.UserName,
                RecipientUsername = reciptient.UserName,
                Content = createMessageDto.Content
            };

            unitOfWork.MessageRepository.AddMessage(message);
            if (await unitOfWork.Complete()) return mapper.Map<MessageDto>(message);
            return BadRequest("Failed to send message");
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageForUser([FromQuery] MessageParams messageParams)
        {
            messageParams.Username = User.GetUserName();
            var messages = await unitOfWork.MessageRepository.GetMessagesForUser(messageParams);
            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages);
            return messages;
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var username = User.GetUserName();
            var message = await unitOfWork.MessageRepository.GetMessage(id);
            if (message == null) return NotFound();
            if (message.Sender.UserName != username && message.Recipient.UserName != username) return Unauthorized();
            if (message.Sender.UserName == username) message.SenderDeleted = true;
            if (message.Recipient.UserName == username) message.RecipientDeleted = true;
            if (message.SenderDeleted && message.RecipientDeleted) unitOfWork.MessageRepository.DeleteMessage(message);
            if (await unitOfWork.Complete()) return Ok();
            return BadRequest("Problem deleting the message");
        }
    }
}