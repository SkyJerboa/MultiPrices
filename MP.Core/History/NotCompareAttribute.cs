using System;

namespace MP.Core.History
{
    [AttributeUsage(AttributeTargets.Property)]
    public class NotCompareAttribute : Attribute
    {
        public NotCompareAttribute()
        {

        }
    }
}
