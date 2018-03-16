using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.UserInterface;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.EditorTools
{
    public interface IMandKTool {
        Texture2D Icon { get; }

        IEnumerable<Button> Buttons { get; }

        void Enable();

        void Disable();

        
    }
}
