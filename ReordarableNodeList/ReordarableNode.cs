using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utilities
{
    public class ReordarableNode : ScriptableObject
    {

        public virtual ReordarableNode Clone()
        {
            return Instantiate(this);
        }
    }
}
