using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MP.Scraping.DTOs;
using MP.Scraping.Models.ServiceGames;
using MP.Scraping.Common.Helpers;
using MP.Scraping.Models.History;
using MP.Core.Contexts.Games;
using System.Security.Claims;
using MP.Scraping.Common.Constants;
using MP.Scraping.Models.Games;
using MP.Scraping.Common.Configuration;

namespace MP.Scraping.Pages
{
    public class ChangesModel : PageModel
    {
        private readonly HistoryContext _context;

        public List<ChangeDTO> Changes { get; set; }

        public ChangesModel(HistoryContext db)
        {
            _context = db;
        }

        public void OnGet()
        {
            Dictionary<int, string> idToNameMap = new Dictionary<int, string>();

            using (GameContext gContext = new GameContext(ScrapingConfigurationManager.Config.SiteConnection))
                idToNameMap = gContext.Games.ToDictionary(k => k.ID, v => v.Name);

            Changes = _context.Changes
                .Where(i => i.ChangedFields != null)
                .GroupBy(i => new { i.GameID, i.ServiceCode })
                .Select(i => new ChangeDTO
                {
                    GameID = i.Key.GameID,
                    GameName = idToNameMap[i.Key.GameID],
                    ServiceCode = i.Key.ServiceCode
                })
                .ToList();
        }

        [FromForm]
        public int? Id { get; set; }
        [FromForm]
        public string fieldName { get; set; }
        [FromForm]
        public int? gameID { get; set; }
        [FromForm]
        public string serviceCode { get; set; }
        [FromForm]
        public string action { get; set; }

        public ActionResult OnPost()
        {
            string role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!UserRoles.ALLOWED_ROLES_EDITING.Contains(role))
                return Forbid();

            List<Change> changes = new List<Change>();

            bool apply = false;
            if (gameID != null && serviceCode != null)
            {
                changes = _context.Changes
                    .Where(i => i.GameID == gameID)
                    .ToList();

                if (action == "Apply")
                    apply = true;
                else
                    _context.Changes.RemoveRange(changes);
            }
            else if (Id != null && fieldName != null)
            {
                Change change = _context.Changes.FirstOrDefault(i => i.ID == Id);
                if (action == "Apply")
                {
                    apply = true;
                    gameID = change.GameID;
                    serviceCode = change.ServiceCode;
                    changes.Add(change);
                }
                else
                {
                    change.ChangedFields = new Dictionary<string, object>(change.ChangedFields.Where(i => i.Key != fieldName));
                    if (change.ChangedFields.Count == 0)
                        _context.Changes.Remove(change);
                }
            }


            if (apply)
                ApplyChanges(changes);

            _context.SaveChanges();

            return StatusCode(200);
        }

        void ApplyChanges(List<Change> changes)
        {
            using (GameWithHistoryContext gService = new GameWithHistoryContext())
            using (ServiceGameContext sgContext = new ServiceGameContext())
            {
                ServiceGame game = sgContext.Games
                    //.Include(i => i.CountryPrices)
                    .Include(i => i.Translations)
                    .FirstOrDefault(i => i.ServiceCode ==  serviceCode.ToUpper() && i.MainGameID == gameID);


                Game mGame = gService.Games
                    .Include(i => i.PriceInfos)
                    .FirstOrDefault(i => i.ID == gameID);

                foreach (Change change in changes)
                {
                    List<string> removingKeys = new List<string>();
                    foreach (var cField in change.ChangedFields)
                    {
                        if (fieldName != null && cField.Key != fieldName)
                            continue;

                        object oItem = null;
                        switch (change.ClassName)
                        {
                            case "Game": oItem = mGame; break;
                            case "ServiceGame": oItem = game; break;
                            case "PriceInfo": oItem = mGame.PriceInfos.FirstOrDefault(i => i.ID == change.ItemID); break;
                            case "SGTranslation": oItem = game.Translations.FirstOrDefault(i => i.ID == change.ItemID); break;
                            case "SGSystemRequirement": oItem = game.SystemRequirements.FirstOrDefault(i => i.ID == change.ItemID); break;
                        }

                        removingKeys.Add(cField.Key);

                        PropertyInfo prop = oItem.GetType().GetProperty(cField.Key);
                        MethodInfo converter = typeof(ConvertHelper).GetMethod("Convert", BindingFlags.Public | BindingFlags.Static);
                        converter = converter.MakeGenericMethod(prop.PropertyType);
                        prop.SetValue(oItem, converter.Invoke(null, new object[] { cField.Value }));
                    }

                    change.ChangedFields = new Dictionary<string, object>(change.ChangedFields.Where(i => !removingKeys.Contains(i.Key)));
                    if (change.ChangedFields.Count == 0)
                        _context.Changes.Remove(change);
                }

                sgContext.SaveChanges(User.Identity.Name);
                gService.SaveChanges(User.Identity.Name);
            }
        }
    }
}