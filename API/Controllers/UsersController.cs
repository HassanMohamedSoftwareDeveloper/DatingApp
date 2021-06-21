using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
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
        [HttpGet()]
        public async Task<ActionResult<IEnumerable<AppUser>>> GetUsers()
        {
            return await DataContext.Users.ToListAsync();
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<AppUser>> GetUsers(int id)
        {
           return await DataContext.Users.FindAsync(id);
        }
        #endregion
    }
}