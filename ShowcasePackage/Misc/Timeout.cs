using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowcasePackage.Misc
{
	class Timeout {

		public bool IsTriggered => Remaining < 0;

		public double Duration { get; private set; }

		public double Remaining { get; private set; }

		public Timeout(double duration)
			:this(duration, duration)
		{
		
		}

		public Timeout(double duration, double remaining)
		{
			if (duration < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(duration), duration, "Timeout time has to be greater than 0");
			}

			if (remaining < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(remaining), remaining, "Timeout remaining time has to be greater than 0");
			}


			Duration = duration;
			Remaining = remaining;
		}

		/// <summary>
		/// Moves the time of the timeout, returns true if expired, false otherwise.
		/// </summary>
		/// <param name="timeStep">How much time has elapsed.</param>
		/// <returns>True if expired, false otherwise.</returns>
		public bool Update(double timeStep)
		{
			if (Remaining < 0) {
				return true;
			}

			Remaining -= timeStep;
			return Remaining < 0;
		}

		/// <summary>
		/// Reset to the <see cref="Duration"/>.
		/// </summary>
		public void Reset()
		{
			Remaining = Duration;
		}
	}
}
