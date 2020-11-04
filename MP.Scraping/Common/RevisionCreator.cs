using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using MP.Scraping.Models.History;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MP.Scraping.Common
{
    public static class RevisionCreator
    {
        public static void CreateRevisions(ChangeTracker changeTracker, List<Type> allowedTypes = null, string user = null)
        {
            List<Revision> revisionList = CreateRevisionsList(changeTracker, allowedTypes, user);
            SaveRevisionsAsync(revisionList);
        }

        public static Task CreateRevisionsTask(ChangeTracker changeTracker, List<Type> allowedTypes = null)
        {
            List<Revision> revisionList = CreateRevisionsList(changeTracker, allowedTypes);
            Task task = new Task(() =>
            {
                using (HistoryContext history = new HistoryContext())
                {
                    history.Revisions.AddRange(revisionList);
                    history.SaveChanges();
                }
            });

            return task;
        }

        private static List<Revision> CreateRevisionsList(ChangeTracker changeTracker, List<Type> allowedTypes = null, string user = null)
        {
            changeTracker.DetectChanges();
            List<Revision> revisionList = new List<Revision>();

            foreach (var entry in changeTracker.Entries())
            {

                if ((allowedTypes == null || !allowedTypes.Contains(entry.Entity.GetType())) &&
                    (entry.State == EntityState.Unchanged || entry.State == EntityState.Added || entry.State == EntityState.Detached))
                    continue;

                IKey keys = entry.Metadata.FindPrimaryKey();
                string keyString = String.Join(',', keys.Properties.Select(i => i.PropertyInfo.GetValue(entry.Entity).ToString()));
                Revision revision = new Revision()
                {
                    Key = keyString,
                    TableName = entry.Metadata.GetTableName(),
                    ClassName = entry.Metadata.ClrType.ToString(),
                    ChangeDate = DateTime.Now,
                    UserName = user
                };

                Dictionary<string, object> oldValues = new Dictionary<string, object>();
                Dictionary<string, object> newValues = new Dictionary<string, object>();

                foreach (PropertyEntry property in entry.Properties)
                {
                    if (property.IsTemporary)
                        continue;

                    string propertyName = property.Metadata.Name;
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            if (!property.IsModified)
                                break;
                            if (property.OriginalValue != null)
                                oldValues.Add(propertyName, property.OriginalValue);
                            newValues.Add(propertyName, property.CurrentValue);
                            break;
                        case EntityState.Deleted:
                            if (property.OriginalValue != null)
                                oldValues.Add(propertyName, property.OriginalValue);
                            break;
                        case EntityState.Added:
                            newValues.Add(propertyName, property.CurrentValue);
                            break;

                    }
                }

                revision.OldValue = (oldValues.Count > 0) ? oldValues : null;
                revision.NewValue = (newValues.Count > 0) ? newValues : null;

                revisionList.Add(revision);
            }

            return revisionList;
        }

        private static async void SaveRevisionsAsync(List<Revision> revisions)
        {
            await Task.Run(() =>
            {
                using (HistoryContext history = new HistoryContext())
                {
                    history.Revisions.AddRange(revisions);
                    history.SaveChanges();
                }
            });
        }
    }
}
