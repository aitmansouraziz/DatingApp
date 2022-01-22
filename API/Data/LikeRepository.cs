using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Data
{
    public class LikeRepository : ILikesRepository
    {
        private readonly DataContext _context;

        public LikeRepository(DataContext context)
        {
            this._context = context;
        }

        public async Task<UserLike> GetUserLike(int sourceUserId, int likedUserId)
        {
            return await _context.Likes.FindAsync(sourceUserId,likedUserId);
        }

        public async Task<PagedList<LikeDto>> GetUserLikes(LikeParams likeParams)
        {
           var users =_context.Users.OrderBy(u=>u.UserName).AsQueryable() ;
            var likes = _context.Likes.AsQueryable();
            if (likeParams.Predicate == "liked")
            {
                likes = likes.Where(like => like.SourceUserId == likeParams.UserId);
                users = likes.Select(like => like.LikeUser);
            }
            if (likeParams.Predicate == "likedBy")
            {
                likes = likes.Where(like => like.LikeUserId == likeParams.UserId);
                users = likes.Select(like => like.SourceUser);
            }
            var likedUser =  users.Select(user => new LikeDto
            {
                Username = user.UserName,
                KnownAs = user.KnownAs,
                Age = user.DateOfBirth.CalculateAge(),
                PhotoUrl = user.Photos.FirstOrDefault(p=>p.IsMain).Url,
                City=user.City
            });

            return await PagedList<LikeDto>.CreateAsync(likedUser, likeParams.PageNumber,likeParams.PageSize);
        }

        public async Task<AppUser> GetUserWithlikes(int userId)
        {
            return await _context.Users
                .Include(x=>x.LikeUsers)
                .FirstOrDefaultAsync(x=>x.Id==userId) ;
        }
    }
}
