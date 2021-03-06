using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        #region Fields :
        private readonly UserManager<AppUser> userManager;
        private readonly SignInManager<AppUser> signInManager;
        private readonly ITokenService tokenService;
        private readonly IMapper mapper;
        #endregion
        #region  CTORS :
        public AccountController(UserManager<AppUser> userManager,SignInManager<AppUser> signInManager, ITokenService tokenService, IMapper mapper)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.tokenService = tokenService;
            this.mapper = mapper;
        }
        #endregion
        #region Actions :
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await UserExists(registerDto.Username)) return BadRequest("Username is taken");
            var user = mapper.Map<AppUser>(registerDto);

            user.UserName = registerDto.Username.ToLower();
           
           var result= await userManager.CreateAsync(user,registerDto.Password);
           if(!result.Succeeded)return BadRequest(result.Errors);
           var roleResult=await userManager.AddToRoleAsync(user,"Member");
            if(!roleResult.Succeeded)return BadRequest(roleResult.Errors);
            return new UserDto
            {
                Username = user.UserName,
                Token =await tokenService.CreateToken(user),
                KnownAs=user.KnownAs
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await userManager.Users.Include(_ => _.Photos)
            .SingleOrDefaultAsync(_ => _.UserName == loginDto.Username.ToLower());
            if (user == null) return NotFound("Invalid username");
            var result=await signInManager.CheckPasswordSignInAsync(user,loginDto.Password,false);
            if(!result.Succeeded)return Unauthorized();
            return new UserDto
            {
                Username = user.UserName,
                Token =await tokenService.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(_ => _.IsMain)?.Url,
                KnownAs=user.KnownAs
            };
        }
        #endregion

        #region Helpers :
        private async Task<bool> UserExists(string userName)
        {
            return await userManager.Users.AnyAsync(_ => _.UserName == userName.ToLower());
        }
        #endregion
    }
}