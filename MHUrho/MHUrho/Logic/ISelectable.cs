using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MHUrho.Logic
{
    interface ISelectable
    {
        bool Select();
        
        //TODO: Other overloads, for clicking buttons etc.
        bool Order(Tile tile);

        bool Order(Unit unit);
        void Deselect();
    }
}