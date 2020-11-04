using MP.Core.History;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace MP.Core.Contexts.History
{
    public class Change
    {
        public int ID { get; set; }
        [MaxLength(5)]
        public string ServiceCode { get; set; }
        [Required]
        public int GameID { get; set; }
        [Required]
        public string ClassName { get; set; }
        [Required]
        public int ItemID { get; set; }

        [NotMapped]
        IVersioning oldVersion { get; }
        private bool canUseApply = false;

        public Dictionary<string, object> ChangedFields
        {
            get { return _changedFields; }
            set
            {
                _changedFields = value;
                HasChanges = (_changedFields?.Count > 0);
            }
        }


        private Dictionary<string, object> _changedFields;
        [NotMapped]
        public bool HasChanges { get; private set; }

        public Change()
        { }

        public Change(IVersioning oldVersion, IVersioning newVersion, string serviceCode, int gameId, ChangeOption option = ChangeOption.None)
        {
            ServiceCode = serviceCode;
            GameID = gameId;
            ClassName = oldVersion.GetType().Name;
            ItemID = oldVersion.ID;
            ChangedFields = VersionControl.GetDifferences(oldVersion, newVersion, option);

            this.oldVersion = oldVersion;
            canUseApply = true;

            //HasChanges = (ChangedFields != null);
        }

        public bool IsSameChange(Change change)
        {
            return change.ServiceCode == ServiceCode && change.GameID == GameID
                && change.ClassName == ClassName && change.ItemID == ItemID;
        }

        //используется только при ранее использованном кастомном конструкторе
        public void ApplyIfNull()
        {
            if (!canUseApply)
                throw new NullReferenceException();

            if (oldVersion == null || ChangedFields == null)
                return;

            List<string> removedKey = new List<string>();
            Type t = oldVersion.GetType();
            foreach (var field in ChangedFields)
            {
                PropertyInfo prop = t.GetProperty(field.Key);
                if (prop.GetValue(oldVersion) == null)
                {
                    prop.SetValue(oldVersion, field.Value);
                    removedKey.Add(field.Key);
                }
            }

            ChangedFields = ChangedFields.Where(i => !removedKey.Contains(i.Key)).ToDictionary(k => k.Key, v => v.Value);
            if (ChangedFields.Count == 0)
                ChangedFields = null;
        }
    }
}
