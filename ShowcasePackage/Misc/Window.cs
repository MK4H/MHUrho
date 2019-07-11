using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowcasePackage.Misc
{
	public abstract class ExclusiveWindow : IDisposable
	{
		public static ExclusiveWindow Current { get; private set; }

		public void Display()
		{
			Current?.Hide();
			Current = this;
			OnDisplay();
		}

		protected virtual void OnDisplay()
		{

		}

		public void Hide()
		{
			OnHide();
			Current = null;
		}

		protected virtual void OnHide()
		{

		}

		public abstract void Dispose();
	}
}
