using System;
using System.Collections.Generic;
using System.Text;


/*
 * Main idea behind these interfaces and a class is to watch the progress of an ongoing task.
 *
 * Classes that execute the ongoing task should implement the IProgressNotifier and notify observers of events and
 * accept optional ProgressWatcher to enable structuring the task into separate subtasks, which the class may be one of
 *
 * Methods that provide updates should accept IProgressEventWatcher and update this watcher from 0 to 100.
 * This watcher should be created on the call to the method and optionally parented to any overarching task progress watcher.
 *
 * Base implementation of Watcher and Notifier is provided in the form of ProgressWatcher class, which accepts updates and
 * distributes them to all registered observers.
 *
 */

namespace MHUrho.Packaging
{
	public interface IProgressNotifier {
		event Action<string> TextUpdate;
		event Action<double> PercentageUpdate;
		event Action<IProgressNotifier> Finished;
		event Action<IProgressNotifier, string> Failed;

		string Text { get; }
		double Percentage { get; }
	}

	public interface IProgressEventWatcher {
		
		void SendTextUpdate(string newText);

		void SendPercentageUpdate(double change);

		void SendUpdate(double percentageChange, string newText);

		void SendFinished();

		void SendSubsectionFinished(IProgressEventWatcher subsection);

		void SendFailed(string message);
	}

	public class ProgressWatcher : IProgressNotifier, IProgressEventWatcher
	{

		readonly IProgressEventWatcher parent;
		readonly double subsectionSize;

		public event Action<string> TextUpdate;
		public event Action<double> PercentageUpdate;
		public event Action<IProgressNotifier> Finished;
		public event Action<IProgressNotifier, string> Failed;

		public string Text { get; private set; }
		public double Percentage { get; private set; }

		/// <summary>
		/// Creates a progress watcher.
		/// Can be used to watch a subsection from 0 to 100%, in which case it will send updates to a <paramref name="parent"/> scaled by <paramref name="size"/>
		/// </summary>
		/// <param name="parent">Parent loading event watcher</param>
		/// <param name="subsectionSize">Size of the subsection in relation to the total progress, range 0-100</param>
		public ProgressWatcher(IProgressEventWatcher parent = null, double subsectionSize = 100)
		{
			if (subsectionSize < 0 || 100 < subsectionSize)
			{
				throw new ArgumentOutOfRangeException(nameof(subsectionSize),
													subsectionSize,
													"Subsection size should be between 0 and 100");
			}

			this.parent = parent;
			this.subsectionSize = subsectionSize / 100;
		}


		public void SendPercentageUpdate(double change)
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

			parent?.SendPercentageUpdate(change * subsectionSize);
		}

		public void SendTextUpdate(string newText)
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

			parent?.SendTextUpdate(newText);
		}

		public void SendUpdate(double percentageChange, string newText)
		{
			SendPercentageUpdate(percentageChange);
			SendTextUpdate(newText);
		}

		public void SendFinished()
		{
			try {
				Finished?.Invoke(this);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(Urho.LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(Finished)}: {e.Message}");
			}

			parent?.SendSubsectionFinished(this);
		}

		public void SendFailed(string message)
		{
			try {
				Failed?.Invoke(this, message);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(Urho.LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(Failed)}: {e.Message}");
			}

			parent?.SendFailed(message);
		}

		public void SendSubsectionFinished(IProgressEventWatcher subsection)
		{

		}
	}
}
