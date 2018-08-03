using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;

namespace MHUrho.UnitComponents
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="button">Pressed button</param>
	/// <param name="buttons">Other buttons down during the button press</param>
	/// <param name="qualifiers">Qualifiers like shift, ctrl, alt etc. </param> TODO: Provide which value is which
	public delegate void ClickedDelegate(int button, int buttons, int qualifiers);

    public class Clicker : DefaultComponent
    {
		internal class Loader : DefaultComponentLoader {

			public override DefaultComponent Component => Clicker;

			public Clicker Clicker { get; private set; }

			public Loader() {

			}

			public static StDefaultComponent SaveState(Clicker clicker)
			{
				var storedClicker = new StClicker
									{
										Enabled = clicker.Enabled
									};
				return new StDefaultComponent {Clicker = storedClicker};
			}

			public override void StartLoading(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData) {

				if (storedData.ComponentCase != StDefaultComponent.ComponentOneofCase.Clicker) {
					throw new ArgumentException("Invalid component type data passed to loader", nameof(storedData));
				}

				var storedClicker = storedData.Clicker;

				Clicker = new Clicker(level)
						{
							Enabled = storedClicker.Enabled
						};
			}

			public override void ConnectReferences(LevelManager level) {

			}

			public override void FinishLoading() {

			}

			public override DefaultComponentLoader Clone() {
				return new Loader();
			}
		}

		public event ClickedDelegate Clicked;

		protected Clicker(ILevelManager level) 
			:base(level)
		{
		}

		public static Clicker CreateNew(ILevelManager level) 
		{
			return new Clicker(level);
		}


		public override StDefaultComponent SaveState()
		{
			return Loader.SaveState(this);
		}

		public void Click(int button, int buttons, int qualifiers)
		{
			Clicked?.Invoke(button, buttons, qualifiers);
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
