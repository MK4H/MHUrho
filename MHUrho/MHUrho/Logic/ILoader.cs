using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
    internal interface ILoader {
        void ConnectReferences(LevelManager level);

        void FinishLoading();
    }
}
