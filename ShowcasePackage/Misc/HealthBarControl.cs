using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.EntityInfo;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;

namespace ShowcasePackage.Misc
{
	class HealthBarControl : IDisposable
	{
		/// <summary>
		/// Percentage of hitpoints remaining.
		/// </summary>
		public double HitPoints { get; private set; }

		/// <summary>
		/// If the health bar is visible
		/// </summary>
		public bool Visible {
			get => visible;
			set {
				if (value) {
					Show();
				}
				else {
					Hide();
				}

				visible = value;
			}
		}

		readonly HealthBar healthBar;
		readonly Vector3 offset;
		readonly Vector2 size;

		bool visible;

		public HealthBarControl(ILevelManager level, IEntity entity, double hitPoints, Vector3 barOffset, Vector2 barSize, bool visible)
		{
			this.HitPoints = hitPoints;
			this.offset = barOffset;
			this.size = barSize;
			healthBar = new HealthBar(level, entity, barOffset, barSize, (float)hitPoints);
			Visible = visible;
		}

		/// <summary>
		/// Set hit points value to <paramref name="newHitPoints"/>.
		/// </summary>
		/// <param name="newHitPoints">New hitpoints value.</param>
		/// <param name="show">If the health bar should be shown.</param>
		/// <returns>True if the <paramref name="newHitPoints"/> value could be set,
		/// false if it was outside the limits of 0 and 100.</returns>
		public bool SetHitpoints(double newHitPoints, bool show = true)
		{
			bool inLimits = true;
			if (newHitPoints > 100) {
				inLimits = false;
				newHitPoints = 100;
			}

			if (newHitPoints < 0) {
				inLimits = false;
				newHitPoints = 0;
			}

			HitPoints = newHitPoints;
			healthBar.SetHealth((float)newHitPoints);
			if (show)
			{
				healthBar.Show();
			}

			return inLimits;
		}

		/// <summary>
		/// Changes the current <see cref="HitPoints"/> by <paramref name="change"/>.
		/// Returns true if the whole change could be made, false if we hit a limit of 0 or 100.
		/// </summary>
		/// <param name="change">Change in the amount of hitpoints..</param>
		/// <param name="show">If the health bar should be shown.</param>
		/// <returns>Returns true if the whole change could be made, false if we hit a limit of 0 or 100.</returns>
		public bool ChangeHitPoints(double change, bool show = true)
		{
			return SetHitpoints(HitPoints + change);
		}

		public void Save(SequentialPluginDataWriter writer)
		{
			writer.StoreNext(offset);
			writer.StoreNext(size);
			writer.StoreNext(HitPoints);
			writer.StoreNext(Visible);
		}

		public static HealthBarControl Load(ILevelManager level, IEntity entity, SequentialPluginDataReader reader)
		{
			reader.GetNext(out Vector3 offset);
			reader.GetNext(out Vector2 size);
			reader.GetNext(out double hp);
			reader.GetNext(out bool visible);
			return new HealthBarControl(level, entity, hp, offset, size, visible);
		}

		protected void Show()
		{
			visible = true;
			healthBar.Show();
		}

		protected void Hide()
		{
			visible = false;
			healthBar.Hide();
		}

		public void Dispose()
		{
			healthBar.Dispose();
		}
	}
}
