using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MP.Scraping.Common;
using MP.Scraping.Common.ServiceScripts;
using MP.Scraping.Models.Services;
using MP.Scraping.Models.Users;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace MP.Scraping.Pages
{
    public class ServiceListModel : PageModel
    {
        ServiceContext _db;

        public List<Service> Services { get; set; }
        public bool IsAdmin { 
            get 
            {
                return User.FindFirst(ClaimTypes.Role)?.Value == UserRole.Admin.ToString();
            }
        }

        public ServiceListModel(ServiceContext context)
        {
            _db = context;
        }

        public IActionResult OnPost([FromQuery]string type)
        {
            bool success;

            switch (type)
            {
                case "compileScripts":
                    success = ScriptLoader.CompileScripts();
                    break;
                case "reloadAssembly":
                    success = ServiceScraper.SafelyReloadAssembly();
                    break;
                default:
                    return new JsonResult(new { success = false, error = "unknown type" });
            }

            return new JsonResult(new { success });
        }
        public void OnGet()
        {
            Services = _db.Services
                .Include(i => i.Requests)
                .ToList();
        }
    }
}