using MHUrho.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Gui;

namespace MHUrho.EditorTools.TerrainManipulation
{
    abstract class TerrainManipulator : IDisposable {

		public abstract void Dispose();

		public virtual void OnEnabled()
		{

		}

		public virtual void OnDisabled()
		{

		}

		public virtual void OnMouseDown(MouseButtonDownEventArgs e)
		{

		}

		public virtual void OnMouseMoved(MHUrhoMouseMovedEventArgs e)
		{

		}

		public virtual void OnMouseUp(MouseButtonUpEventArgs e)
		{

		}
    }
}
