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

			protected MHUrhoApp Game => proxyBase.Game;
			protected MenuUIManager MenuUIManager => proxyBase.MenuUIManager;

			protected ScreenBase(MenuScreen proxy)
			{
				this.proxyBase = proxy;
			}

			public abstract void Dispose();

			/// <summary>
			/// Stores the current state of input and then enables it
			/// </summary>
			public abstract void EnableInput();

			/// <summary>
			/// Stores the current state of input and then disables it
			/// </summary>
			public abstract void DisableInput();

			/// <summary>
			/// Sets the input state to the stored state
			/// </summary>
			public abstract void ResetInput();
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

		protected MHUrhoApp Game => MHUrhoApp.Instance;
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

		/// <summary>
		/// Stores the current state of input and then enables it
		/// </summary>
		public virtual void EnableInput()
		{
			ScreenInstance.EnableInput();
		}

		/// <summary>
		/// Stores the current state of input and then disables it
		/// </summary>
		public virtual void DisableInput()
		{
			ScreenInstance.DisableInput();
		}

		/// <summary>
		/// Sets the input state to the stored state
		/// </summary>
		public virtual void ResetInput()
		{
			ScreenInstance.ResetInput();
		}
	}
}
