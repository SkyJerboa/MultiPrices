using System;

namespace MP.Core.Enums
{
    [Flags]
    public enum Localization
    {
        Unknown = 0,
        Voice = 1,
        Text = 2,
        All = Voice | Text
    }
}
