using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MP.Core.Common.Heplers;
using MP.Core.Contexts.Games;
using MP.Scraping.Common.Configuration;
using MP.Scraping.Models.Games;
using MP.Scraping.Models.ServiceGames;
using System.Collections.Generic;
using System.Linq;
using HSString = System.Collections.Generic.HashSet<System.Collections.Generic.KeyValuePair<string, string>>;

namespace MP.Scraping.Pages
{
    public class TagsModel : PageModel
    {
        public Dictionary<string, List<string>> Tags = new Dictionary<string, List<string>>();

        public void OnGet()
        {
            using (GameContext gContext = new GameContext(ScrapingConfigurationManager.Config.SiteConnection))
            using (ServiceGameContext sgContext = new ServiceGameContext())
            {
                List<string> tags = gContext.Tags.Select(i => i.Name).ToList();
                tags.ForEach(i => Tags.Add(i, new List<string>()));
                HSString tagsMap = sgContext.TagsMap.Select(i => new KeyValuePair<string, string>(i.SourceTag, i.MainTag)).ToHashSet();

                foreach (var tm in tagsMap)
                    Tags[tm.Value].Add(tm.Key);
            }
        }

        public IActionResult OnPost([FromForm] string action)
        {
            switch (action)
            {
                case "pin":
                    string mainTag = Request.Form["mainTag"].ToString();
                    string[] pinTags = Request.Form["pinTags[]"].ToArray();

                    using (GameWithHistoryContext gContext = new GameWithHistoryContext())
                    using (ServiceGameContext sgContext = new ServiceGameContext())
                    {
                        Tag mTag = gContext.Tags.FirstOrDefault(i => i.Name == mainTag);
                        if (mTag == null)
                            return new JsonResult(new { success = false, error = "main Tag not found" });
                        
                        List<GameTagRelation> addedRels = new List<GameTagRelation>();
                        List<int> gameIdsWithMainTag = gContext.GameTagRelations
                            .Where(i => i.TagID == mTag.ID)
                            .Select(i => i.GameID)
                            .ToList();

                        foreach (string ptStr in pinTags)
                        {
                            Tag pt = gContext.Tags.FirstOrDefault(i => i.Name == ptStr);
                            if (pt == null)
                                continue;

                            foreach (GameTagRelation tr in gContext.GameTagRelations.Where(i => i.TagID == pt.ID))
                                if (!gameIdsWithMainTag.Contains(tr.GameID))
                                    addedRels.Add(new GameTagRelation { TagID = mTag.ID, GameID = tr.GameID });

                            sgContext.TagsMap.Add(new TagMap { SourceTag = ptStr, MainTag = mTag.Name });
                            gContext.Tags.Remove(pt);
                        }

                        gContext.GameTagRelations.AddRange(addedRels);

                        gContext.SaveChanges();
                        sgContext.SaveChanges();
                    }
                    return new JsonResult(new { success = true });
                case "unpin":
                    string[] unpinTags = Request.Form["unpinTags[]"].ToArray();

                    using (ServiceGameContext sgContext = new ServiceGameContext())
                    {
                        foreach (string ut in unpinTags)
                        {
                            TagMap tm = sgContext.TagsMap.FirstOrDefault(i => i.SourceTag == ut);
                            if (tm == null)
                                continue;

                            sgContext.Remove(tm);
                        }

                        sgContext.SaveChanges();
                    }
                    break;
                case "add":
                    string aTag = Request.Form["tag"].ToString();
                    using (GameWithHistoryContext gContext = new GameWithHistoryContext())
                    {
                        gContext.Tags.Add(new Tag { Name = aTag.CreateOneLineString() });
                        gContext.SaveChanges();
                    }
                    break;
                case "delete":
                    string[] mTags = Request.Form["mainTags[]"].ToArray();
                    string[] sTags = Request.Form["subTags[]"].ToArray();

                    using (GameWithHistoryContext gContext = new GameWithHistoryContext())
                    using (ServiceGameContext sgContext = new ServiceGameContext())
                    {
                        foreach (string mt in mTags)
                        {
                            Tag t = gContext.Tags.FirstOrDefault(i => i.Name == mt);
                            if (t != null && !sgContext.TagsMap.Any(i => i.MainTag == t.Name))
                                gContext.Tags.Remove(t);
                        }

                        foreach (string st in sTags)
                        {
                            TagMap tm = sgContext.TagsMap.FirstOrDefault(i => i.SourceTag == st);
                            if (tm != null)
                                sgContext.TagsMap.Remove(tm);
                        }

                        gContext.SaveChanges();
                        sgContext.SaveChanges();
                    }
                    break;
                default: return new JsonResult(new { success = false, error = "type not found" });
            }

            return new JsonResult(new { success = true });
        }
    }
}