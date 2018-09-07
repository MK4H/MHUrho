using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Packaging
{

	public interface ILoadingWatcher {
		event Action<float> OnPercentageUpdate;
		event Action<string> OnTextUpdate;
		event Action<ILoadingWatcher> OnFinishedLoading;
		event Action<ILoadingWatcher> OnSubsectionFinishedLoading;

		float Value { get; }
		string Text { get; }
	}

	public interface ILoadingSignaler {
		LoadingWatcher GetWatcherForSubsection(float subsectionSize);

		void TextUpdate(string newText);

		void PercentageUpdate(float change);

		void TextAndPercentageUpdate(string newText, float change);

		void FinishedLoading();
	}

    public class LoadingWatcher : ILoadingWatcher, ILoadingSignaler {

		protected class SubsectionLoadingWatcher : LoadingWatcher {

			/// <summary>
			/// Size of the subsection 0 - 1 represantation of percentage, for easier multiplication
			/// </summary>
			readonly float subsectionSize;
			readonly LoadingWatcher parent;

			public SubsectionLoadingWatcher(float subsectionSize, LoadingWatcher parent)
			{
				this.subsectionSize = subsectionSize / 100;
				this.parent = parent;
			}


			public override void TextUpdate(string newText)
			{
				base.TextUpdate(newText);
				parent.TextUpdate(newText);
			}

			public override void PercentageUpdate(float change)
			{
				base.PercentageUpdate(change);
				parent.PercentageUpdate(change * subsectionSize);
			}

			public override void FinishedLoading()
			{
				base.FinishedLoading();
				parent.SubsectionFinishedLoading(this);
			}
		}

		public event Action<float> OnPercentageUpdate;
		public event Action<string> OnTextUpdate;
		public event Action<ILoadingWatcher> OnFinishedLoading;
		public event Action<ILoadingWatcher> OnSubsectionFinishedLoading;

		public float Value { get; private set; }
		public string Text { get; private set; }

		public LoadingWatcher()
		{
			this.Value = 0;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="subsectionSize">What part of the whole loading process does this subsection represent (1 == 1 percent)</param>
		/// <returns>LoadingWatcher that should be passed to the subsection</returns>
		public LoadingWatcher GetWatcherForSubsection(float subsectionSize)
		{
			return new SubsectionLoadingWatcher(subsectionSize, this);
		}

		public virtual void TextUpdate(string newText)
		{
			MyGame.InvokeOnMainSafe(() => OnTextUpdate?.Invoke(newText));
		}

		public virtual void PercentageUpdate(float change)
		{
			Value += change;
			MyGame.InvokeOnMainSafe(() => { OnPercentageUpdate?.Invoke(Value); });
		}

		public virtual void FinishedLoading()
		{
			MyGame.InvokeOnMainSafe(() => { OnFinishedLoading?.Invoke(this); });
		}

		public void TextAndPercentageUpdate(string newText, float change)
		{
			TextUpdate(newText);
			PercentageUpdate(change);
		}

		protected virtual void SubsectionFinishedLoading(SubsectionLoadingWatcher subsection)
		{
			MyGame.InvokeOnMainSafe(() => { OnSubsectionFinishedLoading?.Invoke(subsection); });
		}
	}
}
