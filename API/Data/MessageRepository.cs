using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext dataContext;
        private readonly IMapper mapper;

        public MessageRepository(DataContext dataContext, IMapper mapper)
        {
            this.dataContext = dataContext;
            this.mapper = mapper;
        }

        public void AddMessage(Message message)
        {
            dataContext.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            dataContext.Messages.Remove(message);
        }

        public async Task<Message> GetMessage(int id)
        {
            return await dataContext.Messages
            .Include(_ => _.Sender).Include(_ => _.Recipient)
            .SingleOrDefaultAsync(_ => _.Id == id);
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = dataContext.Messages.OrderByDescending(m => m.MessageSent).AsQueryable();
            query = messageParams.Container switch
            {
                "Inbox" => query.Where(_ => _.Recipient.UserName == messageParams.Username && _.RecipientDeleted == false),
                "Outbox" => query.Where(_ => _.Sender.UserName == messageParams.Username && _.SenderDeleted == false),
                _ => query.Where(_ => _.Recipient.UserName == messageParams.Username && _.RecipientDeleted == false && _.DateRead == null)
            };
            var messages = query.ProjectTo<MessageDto>(mapper.ConfigurationProvider);
            return await PagedList<MessageDto>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername)
        {
            var messages = await dataContext.Messages.Include(_ => _.Sender).ThenInclude(_ => _.Photos)
            .Include(_ => _.Recipient).ThenInclude(_ => _.Photos)
            .Where(_ => _.Recipient.UserName == currentUsername && _.Sender.UserName == recipientUsername.ToLower() && _.RecipientDeleted == false
            || _.Recipient.UserName == recipientUsername.ToLower() && _.Sender.UserName == currentUsername && _.SenderDeleted == false)
            .OrderBy(_ => _.MessageSent)
            .ToListAsync();


            var unreadMessages = messages.Where(_ => _.DateRead == null && _.Recipient.UserName == currentUsername).ToList();
            if (unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                {
                    message.DateRead = DateTime.Now;
                }
                await dataContext.SaveChangesAsync();
            }

            return mapper.Map<IEnumerable<MessageDto>>(messages);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await dataContext.SaveChangesAsync() > 0;
        }
    }
}