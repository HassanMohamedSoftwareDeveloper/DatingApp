using System;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class BuggyController : BaseApiController
    {
        private readonly DataContext dataContext;
        public BuggyController(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }
        [Authorize]
        [HttpGet("auth")]
        public ActionResult<string> GetSecret()
        {
            return "secret text";
        }
        [HttpGet("not-found")]
        public ActionResult<AppUser> GetNotFound()
        {
            var thing = dataContext.Users.Find(-1);
            return thing == null ? NotFound() : Ok(thing);
        }
        [HttpGet("server-error")]
        public ActionResult<string> GetServerError()
        {
              var thing = dataContext.Users.Find(-1);
                var thingToReturn = thing.ToString();
                return thingToReturn;
            // try
            // {
            //     var thing = dataContext.Users.Find(-1);
            //     var thingToReturn = thing.ToString();
            //     return thingToReturn;
            // }
            // catch (Exception ex)
            // {
            //     return StatusCode(500, "Computer says no!");
            // }

        }
        [HttpGet("bad-request")]
        public ActionResult<string> GetBadRequest()
        {
            return BadRequest("This was not a good request");
        }
    }
}