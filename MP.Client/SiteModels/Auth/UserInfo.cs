using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MP.Client.SiteModels.Auth
{
    public class UserInfo
    {
        public int ID { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool AllowMailing { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}
