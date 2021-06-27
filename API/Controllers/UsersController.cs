using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
   
    public class UsersController : BaseApiController
    {
        #region Fields :
        private readonly DataContext DataContext;
        #endregion
        #region  CTORS :
        public UsersController(DataContext dataContext)
        {
            this.DataContext = dataContext;
        }
        #endregion
        #region Actions :
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppUser>>> GetUsers()
        {
            return await DataContext.Users.ToListAsync();
        }
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<AppUser>> GetUser(int id)
        {
           return await DataContext.Users.FindAsync(id);
        }
        #endregion
    }
}