using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class UsersController : BaseApiController
    {
        #region Fields :
        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;
        private readonly IPhotoService photoService;

        #endregion
        #region  CTORS :
        public UsersController(IUserRepository userRepository, IMapper mapper, IPhotoService photoService)
        {
            this.photoService = photoService;
            this.userRepository = userRepository;
            this.mapper = mapper;
        }
        #endregion
        #region Actions :
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers()
        {
            return Ok(await userRepository.GetMembersAsync());
        }
        [Authorize]
        [HttpGet("{username}", Name = "GetUser")]
        public async Task<ActionResult<MemberDto>> GetUser(string username)
        {
            return await userRepository.GetMemberAsync(username);
        }
        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {
            var user = await userRepository.GetUserByUsernameAsync(User.GetUserName());
            mapper.Map(memberUpdateDto, user);
            userRepository.Update(user);
            if (await userRepository.SaveAllAsync()) return NoContent();
            return BadRequest("Failed to update user");
        }
        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var user = await userRepository.GetUserByUsernameAsync(User.GetUserName());
            var result = await photoService.AddPhotoAsync(file);
            if (result.Error != null) return BadRequest(result.Error.Message);
            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };
            if (user.Photos.Count == 0)
            {
                photo.IsMain = true;
            }
            user.Photos.Add(photo);
            if (await userRepository.SaveAllAsync())
                return CreatedAtRoute("GetUser", new { username = user.UserName }, mapper.Map<PhotoDto>(photo));
            return BadRequest("Problem adding photo");
        }
        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user = await userRepository.GetUserByUsernameAsync(User.GetUserName());
            var photo = user.Photos.FirstOrDefault(_ => _.Id == photoId);
            if (photo.IsMain) return BadRequest("This is already your main photo");
            var currentMain = user.Photos.FirstOrDefault(_ => _.IsMain);
            if (currentMain != null) currentMain.IsMain = false;
            photo.IsMain = true;
            if (await userRepository.SaveAllAsync()) return NoContent();
            return BadRequest("Failed to set main photo");
        }
        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user = await userRepository.GetUserByUsernameAsync(User.GetUserName());
            var photo = user.Photos.FirstOrDefault(_ => _.Id == photoId);
            if (photo == null) return NotFound();
            if (photo.IsMain) return BadRequest("You cann't delete your main photo");
            if (!string.IsNullOrWhiteSpace(photo.PublicId))
            {
                var result = await photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) return BadRequest(result.Error.Message);
            }
            user.Photos.Remove(photo);
            if (await userRepository.SaveAllAsync()) return Ok();
            return BadRequest("Failed to delete the photo");
        }
        #endregion
    }
}