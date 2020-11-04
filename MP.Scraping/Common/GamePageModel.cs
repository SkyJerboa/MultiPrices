using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MP.Core.Common;
using MP.Core.Enums;
using MP.Core.GameInterfaces;
using MP.Scraping.Common.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace MP.Scraping.Common
{
    public abstract class GamePageModel<TGame,TTranslation,TRelation,TImage,TSystemRequirement> : PageModel 
        where TGame : class, IGame
        where TTranslation : class, ITranslation 
        where TRelation : class, IGameRelationship, new()
        where TImage : class, IImage, new() 
        where TSystemRequirement : SystemRequirement
    {
        [FromRoute]
        public int Id { get; set; }

        protected IConfiguration _configuration;
        protected abstract DbContext GameContext { get; }

        public IActionResult OnPost()
        {
            string role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!UserRoles.ALLOWED_ROLES_EDITING.Contains(role))
                return Forbid();

            try
            {
                if (Request.ContentType == "application/json")
                {
                    string body;

                    using (var reader = new StreamReader(Request.Body))
                        body = reader.ReadToEnd();

                    JObject jo = JObject.Parse(body);
                    string type = (string)jo["type"];

                    switch (type)
                    {
                        case "children":
                            string children = jo.Value<string>("ChildID");
                            SaveChildren(children);
                            break;
                        case "game-info":
                            SaveGameInfo(jo["Game"]);
                            break;
                        case "languages":
                            SortedDictionary<string, Localization> langs = jo["Languages"].ToObject<SortedDictionary<string, Localization>>();
                            TGame game = GameContext.Set<TGame>().FirstOrDefault(i => i.ID == Id);
                            if (langs.Count == 0)
                                langs = null;
                            game.Languages = JsonConvert.SerializeObject(langs);
                            break;
                        case "translations":
                            List<TTranslation> translations = jo["Translations"].ToObject<List<TTranslation>>();
                            SaveTranslations(translations);
                            break;
                        case "system-requirements":
                            List<TSystemRequirement> sysReqs = jo["SystemRequirements"].ToObject<List<TSystemRequirement>>();
                            SaveSystemRequirements(sysReqs);
                            break;
                        default:
                            SaveJsonData(jo, type);
                            break;
                    }
                }
                else
                {
                    SaveFormData();
                }

                SaveChanges();
                
                return new OkObjectResult(new { success = true });
            }
            catch(Exception ex)
            {
                return new OkObjectResult(new { success = false, error = ex.Message });
            }

        }

        void SaveChildren(string ids)
        {
            if (String.IsNullOrEmpty(ids))
            {
                GameContext.RemoveRange(GameContext.Set<TRelation>().Where(i => i.ParentID == Id));
                return;
            }

            int[] idArr = ids.Split(',').Select(i => Int32.Parse(i)).ToArray();

            List<int> existingIds = GameContext.Set<TGame>().Where(i => idArr.Contains(i.ID)).Select(i => i.ID).ToList();
            List<TRelation> existingRels = GameContext.Set<TRelation>().Where(i => i.ParentID == Id).ToList();
            List<TRelation> relsToDels = existingRels.Where(i => !existingIds.Contains(i.ChildID)).ToList();
            List<TRelation> relsToAdd = CreateNewGameRelations(existingIds.Where(i => !existingRels.Any(r => i == r.ChildID)));

            GameContext.Set<TRelation>().RemoveRange(relsToDels);
            GameContext.Set<TRelation>().AddRange(relsToAdd);
        }

        protected virtual List<TRelation> CreateNewGameRelations(IEnumerable<int> newChildren)
        {
            return newChildren
                .Select(i => new TRelation
                {
                    ParentID = Id,
                    ChildID = i
                })
                .ToList();
        }

        void SaveTranslations(List<TTranslation> translatons)
        {
            bool findLang(TTranslation t1, TTranslation t2)
            {
                return t1.LanguageCode == t2.LanguageCode && t1.Key == t2.Key;
            }

            List<TTranslation> existingTrans = GameContext.Set<TTranslation>().Where(i => i.GameID == Id).ToList();
            List<TTranslation> transToDelete = existingTrans
                .Where(i => i.GameID == Id && !translatons.Any(t => findLang(t, i)))
                .ToList();
            List<TTranslation> transToAdd = translatons
                .Where(i => !existingTrans.Any(t => findLang(t, i)))
                .ToList();

            transToAdd.ForEach(i => i.GameID = Id);

            foreach (TTranslation trans in existingTrans.Where(i => !transToDelete.Contains(i)))
            {
                TTranslation newTrans = translatons.FirstOrDefault(i => findLang(trans, i));
                trans.Value = newTrans.Value;
            }

            GameContext.Set<TTranslation>().RemoveRange(transToDelete);
            GameContext.Set<TTranslation>().AddRange(transToAdd);
        }
        void SaveSystemRequirements(List<TSystemRequirement> sysReqs)
        {
            bool findSR(TSystemRequirement sr1, TSystemRequirement sr2)
            {
                return sr1.Type == sr2.Type && sr1.SystemType == sr2.SystemType;
            }

            List<TSystemRequirement> existingSR = GameContext.Set<TSystemRequirement>().Where(i => i.GameID == Id).ToList();
            List<TSystemRequirement> srToDelete = existingSR
                .Where(i => !sysReqs.Any(sr => findSR(sr, i)))
                .ToList();
            List<TSystemRequirement> srToAdd = sysReqs
                .Where(i => i.ID == 0 && !existingSR.Any(sr => findSR(sr, i)))
                .ToList();
            srToAdd.ForEach(i => i.GameID = Id);

            foreach (TSystemRequirement sysReq in existingSR.Where(i => !srToDelete.Contains(i)))
            {
                TSystemRequirement newReq = sysReqs.FirstOrDefault(i => findSR(i, sysReq));
                sysReq.CompareAndChange(newReq);
            }

            GameContext.Set<TSystemRequirement>().RemoveRange(srToDelete);
            GameContext.Set<TSystemRequirement>().AddRange(srToAdd);
        }

        /// <summary>
        /// Перемещает все изображени из старой директории в новую
        /// </summary>
        /// <param name="oldAbsolutePath">Абсолютный путь к старой папке</param>
        /// <param name="newAbsolutePath">Абсолютный путь к новой папек</param>
        /// <param name="movedFilesNamesList">Список перемещаемых файлов</param>
        protected void MoveImagesToNewDirectory(string oldAbsolutePath, string newAbsolutePath, List<string> movedFilesNamesList)
        {
            if (!Directory.Exists(oldAbsolutePath))
                return;


            string[] sourceFiles = Directory.GetFiles(oldAbsolutePath);

            if (sourceFiles.Length == 0)
                return;


            if (!Directory.Exists(newAbsolutePath))
                Directory.CreateDirectory(newAbsolutePath);

            foreach (string sourceFile in sourceFiles)
            {
                string fileName = Path.GetFileName(sourceFile);
                if (!movedFilesNamesList.Contains(fileName))
                    continue;

                string destFile = Path.Combine(newAbsolutePath, fileName);

                System.IO.File.Move(sourceFile, destFile);
            }

            if (Directory.GetFiles(oldAbsolutePath).Length == 0)
                Directory.Delete(oldAbsolutePath);

            return;
        }

        /// <summary>
        /// Изменяет свойство Path у изображений в БД, замещая старое имя папки на новое.
        /// </summary>
        /// <param name="oldFolderName">Старое имя директории</param>
        /// <param name="newFolderName">Новое имя дериктории</param>
        /// <returns>Список имен файлов в формате {имя файла}.{расширение}, у которых был изменен путь</returns>
        protected List<string> ChangeImgsPathsInDB(string oldFolderName, string newFolderName)
        {
            List<TImage> sgImagesInDB = GameContext.Set<TImage>().Where(i => i.GameID == Id).ToList();
            List<string> movedFilesNameList = new List<string>();

            int oldFolderNameWithSlashLength = oldFolderName.Length + 1;

            foreach (TImage sgImg in sgImagesInDB.Where(i => i.Path.StartsWith($"{oldFolderName}/{i.Name}")))
            {
                string imgFileName = sgImg.Path.Substring(oldFolderNameWithSlashLength);
                sgImg.Path = $"{newFolderName}/{imgFileName}";
                movedFilesNameList.Add(imgFileName);
            }

            return movedFilesNameList;
        }

        protected abstract void SaveGameInfo(JToken game);
        protected abstract void SaveJsonData(JObject jo, string type);
        protected abstract void SaveFormData();
        protected abstract void SaveChanges();
        protected abstract void MoveImagesAndUpdateDB(string oldFolderName, string newFolderName);
    }
}
