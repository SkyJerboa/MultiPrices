using System;

namespace MP.Scraping.Common.ServiceScripts
{
    public class ScriptValidationException : NullReferenceException
    {
        public ScriptValidationException()
        {
        }

        public ScriptValidationException(string message) : base(message)
        {
        }
    }
}
