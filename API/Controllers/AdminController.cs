using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AdminController : BaseApiController
    {
        private readonly UserManager<AppUser> userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPhotoService photoService;

        public AdminController(UserManager<AppUser> userManager, IUnitOfWork unitOfWork, IPhotoService photoService)
        {
            this.userManager = userManager;
            _unitOfWork = unitOfWork;
            this.photoService = photoService;
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRoles()
        {
            var users = await userManager.Users.Include(_ => _.UserRoles).ThenInclude(_ => _.Role)
            .OrderBy(_ => _.UserName)
            .Select(_ => new
            {
                _.Id,
                username = _.UserName,
                Roles = _.UserRoles.Select(_ => _.Role.Name).ToList()
            }).ToListAsync();
            return Ok(users);
        }
        [HttpPost("edit-roles/{username}")]
        public async Task<ActionResult> EditRoles(string username, [FromQuery] string roles)
        {
            var selectedRoles = roles.Split(',');
            var user = await userManager.FindByNameAsync(username);
            if (user == null) return NotFound("Couldn't find user");
            var userRoles = await userManager.GetRolesAsync(user);
            var result = await userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
            if (!result.Succeeded) return BadRequest("Failed to add roles");
            result = await userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
            if (!result.Succeeded) return BadRequest("Failed to remove from roles");
            return Ok(await userManager.GetRolesAsync(user));

        }
        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photos-to-moderate")]
        public async Task<ActionResult> GetPhotosForModeration()
        {
            var photos = await
           _unitOfWork.PhotoRepository.GetUnapprovedPhotos();
            return Ok(photos);
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("approve-photo/{photoId}")]
        public async Task<ActionResult> ApprovePhoto(int photoId)
        {
            var photo = await
           _unitOfWork.PhotoRepository.GetPhotoById(photoId);
            if (photo == null) return NotFound("Could not find photo");
            photo.IsApproved = true;
            var user = await
           _unitOfWork.UserRepository.GetUserByPhotoId(photoId);
            if (!user.Photos.Any(x => x.IsMain)) photo.IsMain = true;
            await _unitOfWork.Complete();
            return Ok();
        }
        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("reject-photo/{photoId}")]
        public async Task<ActionResult> RejectPhoto(int photoId)
        {
            var photo = await
           _unitOfWork.PhotoRepository.GetPhotoById(photoId);
            if (photo.PublicId != null)
            {
                var result = await
                photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Result == "ok")
                {
                    _unitOfWork.PhotoRepository.RemovePhoto(photo);
                }
            }
            else
            {
                _unitOfWork.PhotoRepository.RemovePhoto(photo);
            }
            await _unitOfWork.Complete();
            return Ok();
        }


    }
}