using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;

namespace MHUrho.DefaultComponents
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="button">Pressed button</param>
	/// <param name="buttons">Other buttons down during the button press</param>
	/// <param name="qualifiers">Qualifiers like shift, ctrl, alt etc. </param>
	public delegate void ClickedDelegate(int button, int buttons, int qualifiers);

    public class Clicker : DefaultComponent
    {
		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => Clicker;

			public Clicker Clicker { get; private set; }

			readonly LevelManager level;
			readonly InstancePlugin plugin;
			readonly StDefaultComponent storedData;

			public Loader() {

			}

			protected Loader(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData)
			{
				this.level = level;
				this.plugin = plugin;
				this.storedData = storedData;
			}

			public static StDefaultComponent SaveState(Clicker clicker)
			{
				var storedClicker = new StClicker
									{
										Enabled = clicker.Enabled
									};
				return new StDefaultComponent {Clicker = storedClicker};
			}

			public override void StartLoading() {

				if (storedData.ComponentCase != StDefaultComponent.ComponentOneofCase.Clicker) {
					throw new ArgumentException("Invalid component type data passed to loader", nameof(storedData));
				}

				var storedClicker = storedData.Clicker;

				Clicker = new Clicker(level)
						{
							Enabled = storedClicker.Enabled
						};
			}

			public override void ConnectReferences() {

			}

			public override void FinishLoading() {

			}

			public override DefaultComponentLoader Clone(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData)
			{
				return new Loader(level, plugin, storedData);
			}
		}

		public event ClickedDelegate Clicked;

		protected Clicker(ILevelManager level) 
			:base(level)
		{
		}

		public static Clicker CreateNew(EntityInstancePlugin plugin, ILevelManager level) 
		{
			var newInstance = new Clicker(level);
			plugin.Entity.AddComponent(newInstance);

			return newInstance;
		}


		public override StDefaultComponent SaveState()
		{
			return Loader.SaveState(this);
		}

		public void Click(int button, int buttons, int qualifiers)
		{
			try {
				Clicked?.Invoke(button, buttons, qualifiers);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(Urho.LogLevel.Warning,
								$"There was an unexpected exception during the invocation of {nameof(Clicked)}: {e.Message}");
			}
		}

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			base.AddedToEntity(entityDefaultComponents);
			AddedToEntity(typeof(Clicker), entityDefaultComponents);

		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(Clicker), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}

	}
}
