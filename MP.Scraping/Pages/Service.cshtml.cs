using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MP.Scraping.Common;
using MP.Scraping.Common.Constants;
using MP.Scraping.Models.Services;

namespace MP.Scraping.Pages
{
    public class ServiceModel : PageModel
    {
        private ServiceContext _db;

        public ServiceModel(ServiceContext context)
        {
            _db = context;
        }

        public Service Service { get; set; }
        public bool Editing { get; set; } = false;
        public ScrapingStatus SStatus { get; set; }

        [FromRoute]
        public string Code { get; set; }
        [FromQuery]
        public string Action { get; set; }

        public IActionResult OnGet()
        {
            if (String.IsNullOrEmpty(Code))
                return NotFound();

            if (!String.IsNullOrEmpty(Action))
            {
                if(Action == "getExc")
                {
                    int id = Int32.Parse(Request.Query["id"]);
                    string exc = _db.ServiceRequests.Find(id)?.Exceptions;
                    return new JsonResult(exc);
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                Service = _db.Services
                    .Include(i => i.Requests)
                    .Include(i => i.SupportedCountries)
                    .FirstOrDefault(i => i.Code == Code.ToUpper());

                if (Service == null)
                    return NotFound();
                else
                {
                    SStatus = ServiceScraper.GetScrapingStatus((string)Service.Code);
                    return Page();
                }
            }
        }

        public IActionResult OnPost([FromForm] bool onlyPrice, [FromForm] bool isTesting)
        {
            string role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!UserRoles.ALLOWED_ROLES_SCRAPING.Contains(role))
                return Forbid();

            if (Action != "run")
                return NotFound();
                
            string cou = Request.Form["country"].ToString();
            string cur = Request.Form["currency"].ToString();
            string lan = Request.Form["lang"].ToString();
            ServiceRequestOptions paramters = 
                new ServiceRequestOptions(cou, cur, lan, onlyPrice, isTesting, User.Identity.Name);

            ServiceScraper.RunScraping(Code, paramters);
            return StatusCode(200);
        }
    }
}