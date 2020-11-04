using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MP.Core.Contexts.Games;
using MP.Scraping.Common.Constants;
using MP.Scraping.Models.Games;
using MP.Scraping.Models.History;

namespace MP.Scraping.Pages
{
    public class RelationChangesModel : PageModel
    {
        HistoryContext _context;

        public List<RelationChange> RelationChanges { get; set; }

        public RelationChangesModel(HistoryContext db)
        {
            _context = db;
        }

        public void OnGet()
        {
            RelationChanges = _context.RelationChanges.ToList();
        }

        public ActionResult OnPost([FromForm]int? id, [FromForm]string action)
        {
            string role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!UserRoles.ALLOWED_ROLES_EDITING.Contains(role))
                return Forbid();

            if (id == null || action == null)
                return Page();

            RelationChange change = _context.RelationChanges.Find(id);

            if (action == "apply")
            {
                using (GameWithHistoryContext gc = new GameWithHistoryContext())
                {
                    gc.GameRelationships.Add(new GameRelationship
                    {
                        ParentID = change.LeftID,
                        ChildID = change.RightID
                    });
                    gc.SaveChanges(User.Identity.Name);
                }

                _context.RelationChanges.Remove(change);
                _context.SaveChanges();
            }
            else if (action == "cancel")
            {

                _context.RelationChanges.Remove(change);
                _context.SaveChanges();
            }

            return StatusCode(200);
        }
    }
}