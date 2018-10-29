using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Packaging
{

	public interface ILoadingWatcher {
		event Action<string> OnTextUpdate;
		event Action<ILoadingWatcher> OnFinishedLoading;
		event Action<ILoadingWatcher> OnSubsectionFinishedLoading;

		string Text { get; }
	}

	public interface ILoadingSignaler {
		LoadingWatcher GetWatcherForSubsection();

		void TextUpdate(string newText);

		void FinishedLoading();
	}

	public class LoadingWatcher : ILoadingWatcher, ILoadingSignaler {

		protected class SubsectionLoadingWatcher : LoadingWatcher {

			readonly LoadingWatcher parent;

			public SubsectionLoadingWatcher(LoadingWatcher parent)
			{
				this.parent = parent;
			}


			public override void TextUpdate(string newText)
			{
				base.TextUpdate(newText);
				parent.TextUpdate(newText);
			}

			public override void FinishedLoading()
			{
				base.FinishedLoading();
				parent.SubsectionFinishedLoading(this);
			}
		}

		public event Action<string> OnTextUpdate;
		public event Action<ILoadingWatcher> OnFinishedLoading;
		public event Action<ILoadingWatcher> OnSubsectionFinishedLoading;

		public string Text { get; private set; }

		public LoadingWatcher()
		{

		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns>LoadingWatcher that should be passed to the subsection</returns>
		public LoadingWatcher GetWatcherForSubsection()
		{
			return new SubsectionLoadingWatcher(this);
		}

		public virtual void TextUpdate(string newText)
		{
			MyGame.InvokeOnMainSafe(() => OnTextUpdate?.Invoke(newText));
		}


		public virtual void FinishedLoading()
		{
			MyGame.InvokeOnMainSafe(() => { OnFinishedLoading?.Invoke(this); });
		}

		protected virtual void SubsectionFinishedLoading(SubsectionLoadingWatcher subsection)
		{
			MyGame.InvokeOnMainSafe(() => { OnSubsectionFinishedLoading?.Invoke(subsection); });
		}
	}
}
