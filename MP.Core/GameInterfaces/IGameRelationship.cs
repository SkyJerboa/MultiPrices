using System;
using System.Collections.Generic;
using System.Text;

namespace MP.Core.GameInterfaces
{
    public interface IGameRelationship
    {
        int ParentID { get; set; }
        int ChildID { get; set; }
    }
}
