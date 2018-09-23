using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.StartupManagement;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    abstract class MenuScreen {

		protected abstract class ScreenBase : IDisposable {

			MenuScreen proxyBase;

			protected MyGame Game => proxyBase.Game;
			protected MenuUIManager MenuUIManager => proxyBase.MenuUIManager;

			protected ScreenBase(MenuScreen proxy)
			{
				this.proxyBase = proxy;
			}

			public abstract void Dispose();
		}

		public bool Visible {
			get => ScreenInstance != null;
			set {
				if (value) {
					Show();
				}
				else {
					Hide();
				}
			}
		}

		protected abstract ScreenBase ScreenInstance { get; set; }

		protected MyGame Game => MyGame.Instance;
		protected readonly MenuUIManager MenuUIManager;

		protected MenuScreen(MenuUIManager menuUIManager)
		{
			this.MenuUIManager = menuUIManager;
		}

		public abstract void ExecuteAction(MenuScreenAction action);

		public abstract void Show();

		public virtual void Hide()
		{
			if (ScreenInstance == null) {
				return;
			}

			ScreenInstance.Dispose();
			ScreenInstance = null;
		}
	}
}
