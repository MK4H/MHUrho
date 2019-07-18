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
	/// <summary>
	/// Consumer end of the progress watching.
	/// </summary>
	public interface IProgressNotifier {
		/// <summary>
		/// Invoked when a text update is sent.
		/// </summary>
		event Action<string> TextUpdate;

		/// <summary>
		/// Invoked when a percentage update is sent.
		/// </summary>
		event Action<double> PercentageUpdate;

		/// <summary>
		/// Invoked when the process is finished.
		/// </summary>
		event Action<IProgressNotifier> Finished;

		/// <summary>
		/// Invoked when the process failed, provides an error message.
		/// </summary>
		event Action<IProgressNotifier, string> Failed;

		/// <summary>
		/// The last text update.
		/// </summary>
		string Text { get; }

		/// <summary>
		/// The last percetnage update value.
		/// </summary>
		double Percentage { get; }
	}

	/// <summary>
	/// Producer end of progress watching.
	/// </summary>
	public interface IProgressEventWatcher {
		
		/// <summary>
		/// Sends text updates with the <paramref name="newText"/> as the value.
		/// </summary>
		/// <param name="newText">The text update value.</param>
		void SendTextUpdate(string newText);

		/// <summary>
		/// Sends a percentage update with the value changed by <paramref name="change"/> from the previous update.
		/// </summary>
		/// <param name="change">The change of the progress of the process.</param>
		void SendPercentageUpdate(double change);

		/// <summary>
		/// Sends combined text and percentage update.
		/// </summary>
		/// <param name="percentageChange">The change of percentage progress.</param>
		/// <param name="newText">The text value of the update.</param>
		void SendUpdate(double percentageChange, string newText);

		/// <summary>
		/// Sends update that the process finished.
		/// </summary>
		void SendFinished();

		/// <summary>
		/// Sends update that part of the process finished.
		/// </summary>
		/// <param name="subsection">The watcher for the part that finished.</param>
		void SendSubsectionFinished(IProgressEventWatcher subsection);

		/// <summary>
		/// Sends update that the process failed.
		/// </summary>
		/// <param name="message">Message describing the failure.</param>
		void SendFailed(string message);
	}

	/// <summary>
	/// Implementation of the process progress watching.
	/// </summary>
	public class ProgressWatcher : IProgressNotifier, IProgressEventWatcher
	{
		/// <summary>
		/// Parent process that this process is part of.
		/// </summary>
		readonly IProgressEventWatcher parent;

		/// <summary>
		/// Size of this process in the parent process.
		/// </summary>
		readonly double subsectionSize;

		/// <inheritdoc />
		public event Action<string> TextUpdate;

		/// <inheritdoc />
		public event Action<double> PercentageUpdate;

		/// <inheritdoc />
		public event Action<IProgressNotifier> Finished;

		/// <inheritdoc />
		public event Action<IProgressNotifier, string> Failed;

		/// <inheritdoc />
		public string Text { get; private set; }

		/// <inheritdoc />
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

		/// <inheritdoc />
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

		/// <inheritdoc />
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

		/// <inheritdoc />
		public void SendUpdate(double percentageChange, string newText)
		{
			SendPercentageUpdate(percentageChange);
			SendTextUpdate(newText);
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
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

		/// <inheritdoc />
		public void SendSubsectionFinished(IProgressEventWatcher subsection)
		{

		}
	}
}
