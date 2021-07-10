using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        #region Fields :
        private readonly DataContext DataContext;
        private readonly ITokenService tokenService;
        #endregion
        #region  CTORS :
        public AccountController(DataContext dataContext, ITokenService tokenService)
        {
            this.DataContext = dataContext;
            this.tokenService = tokenService;
        }
        #endregion
        #region Actions :
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await UserExists(registerDto.Username)) return BadRequest("Username is taken");
            using var hmac = new HMACSHA512();
            var user = new AppUser()
            {
                UserName = registerDto.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };
            DataContext.Users.Add(user);
            await DataContext.SaveChangesAsync();
            return new UserDto
            {
                Username = user.UserName,
                Token = tokenService.CreateToken(user)
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await DataContext.Users.Include(_=>_.Photos)
            .SingleOrDefaultAsync(_ => _.UserName == loginDto.Username.ToLower());
            if (user == null) return NotFound("Invalid username");
            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i])
                    return Unauthorized("Invalid password");
            }
            return new UserDto
            {
                Username = user.UserName,
                Token = tokenService.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(_ => _.IsMain)?.Url
            };
        }
        #endregion

        #region Helpers :
        private async Task<bool> UserExists(string userName)
        {
            return await DataContext.Users.AnyAsync(_ => _.UserName == userName.ToLower());
        }
        #endregion
    }
}