using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;

namespace MHUrho.Threading
{
	/// <summary>
	/// Synchronization context that sends and posts everything onto the main thread of the Urho3D library
	///
	/// Useful for Continuations that work with UI or Scene
	/// </summary>
	public class MHUrhoSynchronizationContext : SynchronizationContext
	{
		public override SynchronizationContext CreateCopy()
		{
			return new MHUrhoSynchronizationContext();
		}

		public override void Post(SendOrPostCallback d, object state)
		{
			MyGame.InvokeOnMainSafeAsync(() => d(state));
		}

		public override void Send(SendOrPostCallback d, object state)
		{
			MyGame.InvokeOnMainSafe(() => d(state));
		}
	}
}
