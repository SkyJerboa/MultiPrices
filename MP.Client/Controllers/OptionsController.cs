using Microsoft.AspNetCore.Mvc;
using MP.Client.Contexts;

namespace MP.Client.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class OptionsController : Controller
    {
        private MainContext db;

        public OptionsController(MainContext context)
        {
            db = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return new JsonResult(db.Services);
        }
    }
}
