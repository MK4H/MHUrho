using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MHUrho.Logic
{
    public interface ISelectable
    {
        bool Select();
        
        //TODO: Other overloads, for clicking buttons etc.
        bool Order(ITile tile);

        bool Order(Unit unit);
        void Deselect();
    }
}