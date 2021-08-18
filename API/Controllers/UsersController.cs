using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class UsersController : BaseApiController
    {
        #region Fields :
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly IPhotoService photoService;

        #endregion
        #region  CTORS :
        public UsersController(IUnitOfWork unitOfWork, IMapper mapper, IPhotoService photoService)
        {
            this.photoService = photoService;
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }
        #endregion
        #region Actions :
        //[Authorize(Roles ="Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers([FromQuery] UserParams userParams)
        {
            var gender = await unitOfWork.UserRepository.GetUserGender(User.GetUserName());
            userParams.CurrentUsername = User.GetUserName();
            if (string.IsNullOrWhiteSpace(userParams.Gender)) userParams.Gender = gender == "male" ? "female" : "male";
            var users = await unitOfWork.UserRepository.GetMembersAsync(userParams);
            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);
            return Ok(users);
        }
        //[Authorize(Roles ="Member")]
        [HttpGet("{username}", Name = "GetUser")]
        public async Task<ActionResult<MemberDto>> GetUser(string username)
        {
            var currentUsername = User.GetUserName();
            return await unitOfWork.UserRepository.GetMemberAsync(username,
            isCurrentUser: currentUsername == username);
        }
        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {
            var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUserName());
            mapper.Map(memberUpdateDto, user);
            unitOfWork.UserRepository.Update(user);
            if (await unitOfWork.Complete()) return NoContent();
            return BadRequest("Failed to update user");
        }
        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var user = await
           unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUserName());
            var result = await photoService.AddPhotoAsync(file);
            if (result.Error != null) return BadRequest(result.Error.Message);
            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };
            user.Photos.Add(photo);
            if (await unitOfWork.Complete())
            {
                return CreatedAtRoute("GetUser", new
                {
                    username =
               user.UserName
                }, mapper.Map<PhotoDto>(photo));
            }
            return BadRequest("Problem addding photo");
        }
        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUserName());
            var photo = user.Photos.FirstOrDefault(_ => _.Id == photoId);
            if (photo.IsMain) return BadRequest("This is already your main photo");
            var currentMain = user.Photos.FirstOrDefault(_ => _.IsMain);
            if (currentMain != null) currentMain.IsMain = false;
            photo.IsMain = true;
            if (await unitOfWork.Complete()) return NoContent();
            return BadRequest("Failed to set main photo");
        }
        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUserName());
            var photo = user.Photos.FirstOrDefault(_ => _.Id == photoId);
            if (photo == null) return NotFound();
            if (photo.IsMain) return BadRequest("You cann't delete your main photo");
            if (!string.IsNullOrWhiteSpace(photo.PublicId))
            {
                var result = await photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) return BadRequest(result.Error.Message);
            }
            user.Photos.Remove(photo);
            if (await unitOfWork.Complete()) return Ok();
            return BadRequest("Failed to delete the photo");
        }
        #endregion
    }
}