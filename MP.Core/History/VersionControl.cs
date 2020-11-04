using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;

namespace MP.Core.History
{
    public static class VersionControl
    {
        private static readonly List<Type> _basTypes = new List<Type>()
        {
            typeof(string), typeof(int), typeof(float), typeof(DateTime), typeof(bool),
            typeof(int?), typeof(float?), typeof(DateTime?)
        };

        //public static Dictionary<string, object> GetDifferences(object oldVersion, object newVersion, ChangeOption option = ChangeOption.None)
        //    => GetDifferences(oldVersion, newVersion, option);

        public static void ApplyChanges(object oldVersion, object newVersion, ChangeOption option)
            => GetDifferences(oldVersion, newVersion, option);

        public static Dictionary<string, object> GetDifferences(object oldVersion, object newVersion, ChangeOption option = ChangeOption.None)
        {
            if (oldVersion.GetType() != newVersion.GetType())
                return null;

            Dictionary<string, object> differences = new Dictionary<string, object>();

            PropertyInfo[] properties = oldVersion.GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (!IsComparableProperty(property))
                    continue;


                object valueOld = property.GetValue(oldVersion);
                object valueNew = property.GetValue(newVersion);

                if ((option == ChangeOption.SkipNull || option == ChangeOption.ExcludeNull) && valueNew == null)
                    continue;

                if (!Object.Equals(valueOld, valueNew))
                {
                    differences.Add(property.Name, valueNew);
                    if (option > 0)
                    {
                        if (option == ChangeOption.Apply || 
                            ((option == ChangeOption.ApplyIfNull || option == ChangeOption.ExcludeNull) && valueOld == null && valueNew != null))
                            property.SetValue(oldVersion, valueNew);
                    }
                }
            }

            return (differences.Count > 0) ? differences : null;
        }


        private static bool IsComparableProperty(PropertyInfo prop)
        {
            return prop.GetCustomAttribute(typeof(NotCompareAttribute)) == null && prop.GetCustomAttribute(typeof(NotMappedAttribute)) == null
                && (_basTypes.Contains(prop.PropertyType) || prop.PropertyType.IsEnum) && prop.Name != "ID";
        }
    }

    public enum ChangeOption
    {
        None = 0,
        ApplyIfNull = 1,
        Apply = 2,
        SkipNull = 3,
        ExcludeNull = 4 //заменяет старое значение, если оно null и не применяет новое, если оно null
    }
}
