using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.UserInterface;
using Urho.Gui;

namespace MHUrho.EditorTools
{
    public interface IMandKTool {
        IEnumerable<Button> Buttons { get; }

        void Enable();

        void Disable();

        
    }
}
