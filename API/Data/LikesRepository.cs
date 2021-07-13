using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class LikesRepository : ILikesRepository
    {
        private readonly DataContext dataContext;
        public LikesRepository(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }
        public async Task<UserLike> GetUserLike(int sourceUserId, int likedUserId)
        {
            return await dataContext.Likes.FindAsync(sourceUserId, likedUserId);
        }
        public async Task<PagedList<LikeDto>> GetUserLikes(LikesParams likesParams)
        {
            var users = dataContext.Users.OrderBy(_ => _.UserName).AsQueryable();
            var likes = dataContext.Likes.AsQueryable();

            if (likesParams.Predicate == "liked")
            {
                likes = likes.Where(_ => _.SourceUserId == likesParams.UserId);
                users = likes.Select(_ => _.LikedUser);
            }

            if (likesParams.Predicate == "likedBy")
            {
                likes = likes.Where(_ => _.LikedUserId == likesParams.UserId);
                users = likes.Select(_ => _.SourceUser);
            }
            var likedUsers = users.Select(user => new LikeDto
            {
                Username = user.UserName,
                KnownAs = user.KnownAs,
                Age = user.DateOfBirth.CalculateAge(),
                PhotoUrl = user.Photos.FirstOrDefault(_ => _.IsMain).Url,
                City = user.City,
                Id = user.Id
            });
            return await PagedList<LikeDto>.CreateAsync(likedUsers, likesParams.PageNumber, likesParams.PageSize);
        }
        public async Task<AppUser> GetUserwithLikes(int userId)
        {
            return await dataContext.Users.Include(_ => _.LikedUsers).FirstOrDefaultAsync(_ => _.Id == userId);
        }
    }
}