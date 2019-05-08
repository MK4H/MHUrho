using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Packaging
{

	public interface ILoadingWatcher {
		event Action<string> TextUpdate;
		event Action<double> PercentageUpdate;
		event Action<ILoadingWatcher> FinishedLoading;
		event Action<ILoadingWatcher> SubsectionFinishedLoading;

		string Text { get; }
		double Percentage { get; }
	}

	public interface ILoadingProgress {
		
		/// <summary>
		/// Creates a loading watcher for a subsection of the current loading section.
		/// Subsection has a certain <paramref name="subsectionSize"/>, when the subsection is 100% finished,
		/// the parent section will be advanced by 100*<paramref name="subsectionSize"/>%.
		/// </summary>
		/// <param name="subsectionSize">Size of the subsection in relation to the parent section of loading, 0 to 100% of the parent</param>
		/// <returns>LoadingWatcher that should be passed to the subsection</returns>
		LoadingWatcher GetWatcherForSubsection(double subsectionSize);

		void SendTextUpdate(string newText);

		void SendPercentageUpdate(double change);

		void SendUpdate(double percentageChange, string newText);

		void SendFinishedLoading();
	}

	public class LoadingWatcher : ILoadingWatcher, ILoadingProgress {

		protected class SubsectionLoadingWatcher : LoadingWatcher {

			readonly LoadingWatcher parent;
			readonly double subsectionSize;

			/// <summary>
			/// Creates a loading watcher for a subsection, so the subsection can
			/// count it's own progress from 0 to 100% independently.
			/// </summary>
			/// <param name="parent">Parent loading watcher, even a subsection</param>
			/// <param name="subsectionSize">Size of the subsection in relation to the total loading process, range 0-100</param>
			public SubsectionLoadingWatcher(LoadingWatcher parent, double subsectionSize)
			{
				if (subsectionSize < 0 || 100 < subsectionSize) {
					throw new ArgumentOutOfRangeException(nameof(subsectionSize),
														subsectionSize,
														"Subsection size should be between 0 and 100");
				}

				this.parent = parent;
				this.subsectionSize = subsectionSize / 100;
			}


			public override void SendPercentageUpdate(double change)
			{
				base.SendPercentageUpdate(change);
				parent.SendPercentageUpdate(change * subsectionSize);
			}

			public override void SendTextUpdate(string newText)
			{
				base.SendTextUpdate(newText);
				parent.SendTextUpdate(newText);
			}

			public override void SendFinishedLoading()
			{
				base.SendFinishedLoading();
				parent.SendSubsectionFinishedLoading(this);
			}
		}

		public event Action<string> TextUpdate;
		public event Action<double> PercentageUpdate;
		public event Action<ILoadingWatcher> FinishedLoading;
		public event Action<ILoadingWatcher> SubsectionFinishedLoading;

		public string Text { get; private set; }
		public double Percentage { get; private set; }

		public LoadingWatcher()
		{

		}


		/// <summary>
		/// Creates a loading watcher for a subsection of the current loading section.
		/// Subsection has a certain <paramref name="subsectionSize"/>, when the subsection is 100% finished,
		/// the parent section will be advanced by 100*<paramref name="subsectionSize"/>%.
		/// </summary>
		/// <param name="subsectionSize">Size of the subsection in relation to the parent section of loading, 0 to 100% of the parent</param>
		/// <returns>LoadingWatcher that should be passed to the subsection</returns>
		public LoadingWatcher GetWatcherForSubsection(double subsectionSize)
		{
			return new SubsectionLoadingWatcher(this, subsectionSize);
		}

		public virtual void SendPercentageUpdate(double change)
		{
			Percentage += change;
			try {
				PercentageUpdate?.Invoke(Percentage);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(Urho.LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(PercentageUpdate)}: {e.Message}");
			}
		}

		public virtual void SendTextUpdate(string newText)
		{
			Text = newText;
			//Possible user methods
			try {
				TextUpdate?.Invoke(newText);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(Urho.LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(TextUpdate)}: {e.Message}");
			}
		}

		public void SendUpdate(double percentageChange, string newText)
		{
			SendPercentageUpdate(percentageChange);
			SendTextUpdate(newText);
		}

		public virtual void SendFinishedLoading()
		{
			try {
				FinishedLoading?.Invoke(this);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(Urho.LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(FinishedLoading)}: {e.Message}");
			}

		}

		protected virtual void SendSubsectionFinishedLoading(SubsectionLoadingWatcher subsection)
		{
			try {
				SubsectionFinishedLoading?.Invoke(subsection);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(Urho.LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(SubsectionFinishedLoading)}: {e.Message}");
			}
		}
	}
}
