using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho;
using MHUrho.Plugins;
using Urho.Gui;

namespace ShowcasePackage.Levels
{
	public class ResourceCreationWindow : LevelLogicCustomSettings {


		public int Value { get; private set; }
	

		readonly Slider resourceSlider;

		public ResourceCreationWindow(Window settingsWindow, MHUrhoApp game)
		{
			game.UI.LoadLayoutToElement(settingsWindow, game.ResourceCache, "Assets/UI/CreationWindowLayout.xml");
			resourceSlider = (Slider)settingsWindow.GetChild("ResourceSlider", true);
			resourceSlider.SliderChanged += OnSliderChanged;
		}

		public override void Dispose()
		{
			resourceSlider.SliderChanged -= OnSliderChanged;
			resourceSlider.Dispose();
		}

		void OnSliderChanged(SliderChangedEventArgs obj)
		{
			Value = (int)Math.Round(obj.Value);
			((Slider)obj.Element).Value = Value;
		}
	}
}
