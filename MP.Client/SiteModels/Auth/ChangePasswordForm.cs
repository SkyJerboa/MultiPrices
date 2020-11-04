using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MP.Client.SiteModels.Auth
{
    public class ChangePasswordForm
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string PasswordConfirm { get; set; }
    }
}
