using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseApiController : ControllerBase
    {
        // #region Fields :
        // private readonly DataContext DataContext;
        // #endregion
        // #region  CTORS :
        // public BaseApiController(DataContext dataContext)
        // {
        //     this.DataContext = dataContext;
        // }
        // #endregion
    }
}