using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Logic
{
    public class Entity : Component
    {
        public new int ID { get; set; }

        public IPlayer Player { get; protected set; }
    }
}
