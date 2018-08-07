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

		public virtual void OnMouseDown(MouseButtonDownEventArgs args)
		{

		}

		public virtual void OnMouseMoved(MHUrhoMouseMovedEventArgs args)
		{

		}

		public virtual void OnMouseUp(MouseButtonUpEventArgs args)
		{

		}

		public virtual void OnCameraMove(CameraMovedEventArgs args)
		{

		}
	}
}
