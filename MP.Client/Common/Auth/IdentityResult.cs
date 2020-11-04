using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MP.Client.Common.Auth
{
    public class IdentityResult
    {
        private static readonly IdentityResult _success = new IdentityResult { Successful = true };
        
        public bool Successful { get; private set; }

        public static IdentityResult Success = _success;

        public IdentityError Error { get; private set; }

        public IdentityResult(IdentityError error = null)
        {
            if (error == null)
                Successful = true;
            else
                Error = error;
        }

        public IdentityResult(string error, string message = null, string code = null)
        {
            Error = new IdentityError(error, message, code);
        }
    }
}
