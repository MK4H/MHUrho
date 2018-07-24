using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Packaging
{
    public class LoadingWatcher {
		public static LoadingWatcher Ignoring => new LoadingWatcher(null, null, null);

		readonly Action<float> percentageUpdate;
		readonly Action<string> phaseUpdate;

		readonly Action<LoadingWatcher> finishedLoading;

		float value;

		public LoadingWatcher(Action<float> percentageUpdate, Action<string> phaseUpdate, Action<LoadingWatcher> finishedLoading)
		{
			this.value = 0;
			this.percentageUpdate = percentageUpdate;
			this.phaseUpdate = phaseUpdate;
			this.finishedLoading = finishedLoading;
		}

		public void EnterPhase(string phaseName)
		{
			MyGame.InvokeOnMainSafe(() => { phaseUpdate?.Invoke(phaseName); });
			
		}

		public void IncrementProgress(float change)
		{
			value += change;
			MyGame.InvokeOnMainSafe(() => { percentageUpdate?.Invoke(value); });
		}

		public void EnterPhaseWithIncrement(string phaseName, float change)
		{
			EnterPhase(phaseName);
			IncrementProgress(change);
		}

		public void FinishedLoading()
		{
			MyGame.InvokeOnMainSafe(() => { finishedLoading?.Invoke(this); });
		}
	}
}
