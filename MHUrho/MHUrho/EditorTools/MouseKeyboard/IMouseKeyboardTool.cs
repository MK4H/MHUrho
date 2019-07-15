using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.UserInterface;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.EditorTools.MouseKeyboard
{
	public interface IMouseKeyboardTool {
		IntRect IconRectangle { get; }

		void Enable();

		void Disable();

		
	}
}
