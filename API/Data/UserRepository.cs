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
    public class UserRepository : IUserRepository
    {
        private readonly DataContext dataContext;
        private readonly IMapper mapper;
        public UserRepository(DataContext dataContext, IMapper mapper)
        {
            this.dataContext = dataContext;
            this.mapper = mapper;
        }
        public async Task<MemberDto> GetMemberAsync(string username, bool isCurrentUser)
        {
            var query = dataContext.Users
            .Where(x => x.UserName == username)
            .ProjectTo<MemberDto>(mapper.ConfigurationProvider)
            .AsQueryable();
            if (isCurrentUser) query = query.IgnoreQueryFilters();
            return await query.FirstOrDefaultAsync();
        }
        public async Task<PagedList<MemberDto>> GetMembersAsync(UserParams userParams)
        {
            var query = dataContext.Users.AsQueryable();
            query = query.Where(_ => _.UserName != userParams.CurrentUsername);
            query = query.Where(_ => _.Gender == userParams.Gender);
            var mindob = DateTime.Today.AddYears(-userParams.Maxge - 1);
            var maxdob = DateTime.Today.AddYears(-userParams.MinAge);
            query = query.Where(_ => _.DateOfBirth >= mindob && _.DateOfBirth <= maxdob);

            query = userParams.OrderBy switch
            {
                "created" => query.OrderByDescending(_ => _.Created),
                _ => query.OrderByDescending(_ => _.LastActive)
            };

            return await PagedList<MemberDto>.CreateAsync(query.ProjectTo<MemberDto>(mapper.ConfigurationProvider).AsNoTracking(),
             userParams.PageNumber, userParams.PageSize);
        }
        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await dataContext.Users.FindAsync(id);
        }
        public async Task<AppUser> GetUserByUsernameAsync(string username)
        {
            return await dataContext.Users.Include(_ => _.Photos).SingleOrDefaultAsync(_ => _.UserName == username.ToLower());
        }
        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            return await dataContext.Users.Include(_ => _.Photos).ToListAsync();
        }
        public void Update(AppUser user)
        {
            dataContext.Entry(user).State = EntityState.Modified;
        }
        public async Task<string> GetUserGender(string username)
        {
            return (await dataContext.Users.Where(_ => _.UserName == username).Select(x => x.Gender).FirstOrDefaultAsync());
        }

        public async Task<AppUser> GetUserByPhotoId(int photoId)
        {
            return await dataContext.Users
            .Include(p => p.Photos)
            .IgnoreQueryFilters()
            .Where(p => p.Photos.Any(p => p.Id == photoId))
            .FirstOrDefaultAsync();
        }
    }
}