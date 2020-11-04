using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MP.Core.Contexts.History
{
    public class Revision
    {
        public int ID { get; set; }
        public string TableName { get; set; }
        [Required]
        public string ClassName { get; set; }
        public DateTime ChangeDate { get; set; }
        public Dictionary<string, object> OldValue { get; set; }
        public Dictionary<string, object> NewValue { get; set; }
    }
}
