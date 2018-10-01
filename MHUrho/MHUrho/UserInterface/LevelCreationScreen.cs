using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.StartupManagement;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class LevelCreationScreen : MenuScreen {
		class Screen : ScreenBase {

			class SliderDuo : IDisposable {

				public IntVector2 Value {
					get => new IntVector2(X, Y);
					set {
						X = value.X;
						Y = value.Y;
					}
				} 

				public int X {
					get => SliderValueToRealValue(sliderX.Value, minValue.X, step.X);
					set => sliderX.Value = RealValueToSliderValue(value, minValue.X, step.X);
				}
				public int Y {
					get => SliderValueToRealValue(sliderY.Value, minValue.Y, step.Y);
					set => sliderY.Value = RealValueToSliderValue(value, minValue.Y, step.Y);
				}


				public bool Enabled {
					get => sliderX.Enabled && sliderY.Enabled;
					set {
						if (value) {
							Enable();
						}
						else {
							Disable();
						}
					}
				}

				readonly Slider sliderX;
				readonly Slider sliderY;
				readonly Text displayText;

				readonly IntVector2 step;

				readonly IntVector2 minValue;

				public SliderDuo(Slider sliderX, 
								Slider sliderY,
								Text displayText, 
								IntVector2 minValue, 
								IntVector2 maxValue, 
								IntVector2 step,
								IntVector2 initialValue)
				{
					this.sliderX = sliderX;
					this.sliderY = sliderY;
					this.displayText = displayText;
					this.minValue = minValue;
					this.step = step;

					sliderX.Range = (maxValue.X - minValue.X) / step.X;
					sliderY.Range = (maxValue.Y - minValue.Y) / step.X;

					sliderX.SliderChanged += SliderValueChanged;
					sliderY.SliderChanged += SliderValueChanged;

					Value = initialValue;

					UpdateDisplayText();
				}

				

				public void Dispose()
				{
					sliderX.SliderChanged -= SliderValueChanged;
					sliderY.SliderChanged -= SliderValueChanged;

					sliderX.Dispose();
					sliderY.Dispose();
					displayText.Dispose();
				}

				public void Enable()
				{
					if (Enabled) {
						return;
					}

					sliderX.Enabled = true;
					sliderY.Enabled = true;
				}

				public void Disable() {
					if (!Enabled) {
						return;
					}

					sliderX.Enabled = false;
					sliderY.Enabled = false;
				}

				void SliderValueChanged(SliderChangedEventArgs args)
				{
					//Round the value to nearest step
					if (args.Element == sliderX) {
						sliderX.Value = (float)Math.Round(args.Value);
					}
					else if (args.Element == sliderY) {
						sliderY.Value = (float)Math.Round(args.Value);
					}
					else {
						throw new ArgumentException("Unknown element in args.Element", nameof(args));
					}

					UpdateDisplayText();
				}

				int SliderValueToRealValue(float value, int minValue, int stepSize)
				{
					return minValue + ((int)Math.Round(value)) * stepSize;
				}

				float RealValueToSliderValue(int value, int minValue, int stepSize)
				{
					return (value - minValue) / (float) stepSize;
				}

				void UpdateDisplayText()
				{
					displayText.Value = $"{X}x{Y}";
				}
			}

			

			readonly LevelCreationScreen proxy;

			LevelRep Level {
				get => proxy.Level;
				set => proxy.Level = value;
			}

			string Name {get;set;}

			string Description { get; set; }

			string ThumbnailPath => thumbnailPathText.Value;

			//TODO: Add plugin name to pick the correct plugin from the assembly
			string PluginPath => pluginPathText.Value;

			readonly Window window;
			readonly LineEdit nameEdit;
			readonly SliderDuo mapSize;
			readonly Button pluginPathButton;
			readonly PathText pluginPathText;
			readonly Button thumbnailPathButton;
			readonly PathText thumbnailPathText;
			readonly LineEdit descriptionEdit;

			public Screen(LevelCreationScreen proxy)
				:base(proxy)
			{
				this.proxy = proxy;

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/LevelCreationLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild("LevelCreationWindow");

				nameEdit = (LineEdit)window.GetChild("LevelNameEdit", true);
				
				var sliderX = (Slider) window.GetChild("XSlider", true);
				var sliderY = (Slider) window.GetChild("ZSlider", true);
				var displayText = (Text) window.GetChild("DisplayText", true);
				mapSize = new SliderDuo(sliderX, sliderY, displayText, Map.MinSize, Map.MaxSize, Map.ChunkSize, Map.MinSize);

				pluginPathButton = (Button) window.GetChild("LevelPluginButton", true);
				thumbnailPathButton = (Button) window.GetChild("ThumbnailPathButton", true);

				pluginPathText = new PathText((Text)pluginPathButton.GetChild("PluginPathText"));
				thumbnailPathText = new PathText((Text) thumbnailPathButton.GetChild("ThumbnailPathText"));


				descriptionEdit = (LineEdit) window.GetChild("DescriptionEdit", true);


				nameEdit.TextChanged += NameChanged;
				descriptionEdit.TextChanged += DescriptionChanged;
				pluginPathButton.Released += PluginPathButtonButtonReleased;
				thumbnailPathButton.Released += ThumbnailPathButtonReleased;

				((Button)window.GetChild("EditButton", true)).Released += EditButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released += BackButtonReleased;


				//Editing existing level, load the values and lock the unchangable ones (levelSize and plugin)
				if (Level != null) {
					nameEdit.Text = Level.Name;
					mapSize.Value = Level.MapSize;
					descriptionEdit.Text = Level.Description;
					thumbnailPathText.Value = Level.ThumbnailPath;
					pluginPathText.Value = Level.LevelPluginAssemblyPath;

					//Disable the unchangable ones
					pluginPathButton.Enabled = false;
					mapSize.Enabled = false;
				}
			}

			public override void Dispose()
			{
				nameEdit.TextChanged -= NameChanged;
				descriptionEdit.TextChanged -= DescriptionChanged;
				pluginPathButton.Released -= PluginPathButtonButtonReleased;
				thumbnailPathButton.Released -= ThumbnailPathButtonReleased;

				((Button)window.GetChild("EditButton", true)).Released -= EditButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released -= BackButtonReleased;

				nameEdit.Dispose();
				mapSize.Dispose();
				pluginPathButton.Dispose();
				thumbnailPathButton.Dispose();
				descriptionEdit.Dispose();

				window.RemoveAllChildren();
				window.Remove();
				window.Dispose();
			}

			void EditButtonReleased(ReleasedEventArgs args)
			{

				//Creating new level
				if (proxy.Level == null) {
					proxy.Level = LevelRep.CreateNewLevel(Name,
														Description,
														ThumbnailPath,
														PluginPath,
														mapSize.Value,
														PackageManager.Instance.ActivePackage);
				}

				MenuUIManager.MenuController.StartLoadingLevelForEditing(proxy.Level);
				
			}

			void BackButtonReleased(ReleasedEventArgs args)
			{
				MenuUIManager.SwitchBack();
			}

			void NameChanged(TextChangedEventArgs args)
			{
				if (!LevelRep.IsNameValid(args.Text)) {
					nameEdit.Text = Name;
				}
				Name = nameEdit.Text;
			}

			void DescriptionChanged(TextChangedEventArgs args)
			{
				if (!LevelRep.IsDescriptionValid(args.Text)) {
					descriptionEdit.Text = Description;
				}
				Description = descriptionEdit.Text;
			}

			void PluginPathButtonButtonReleased(ReleasedEventArgs args)
			{
				RequestPathToText(pluginPathText);
			}

			void ThumbnailPathButtonReleased(ReleasedEventArgs args)
			{
				RequestPathToText(thumbnailPathText);
			}

			async void RequestPathToText(PathText pathText)
			{
				try {
					string oldText = pathText.Value;

					var result = await MenuUIManager.
										FileBrowsingPopUp.
										Request(PackageManager.Instance.ActivePackage.RootedDirectoryPath,
												SelectOption.File,
												 pathText.HasDefaultValue ? null : pathText.Value);
					

					string newText = result == null ? oldText : result.RelativePath;
					MyGame.InvokeOnMainSafeAsync(() => pathText.Value = newText);
				}
				catch (OperationCanceledException e) {
					//Text should not have changed
				}
			}

#if DEBUG
			public void SimulateEditNewLevel(string name, string description, string thumbnailPath, string pluginPath, IntVector2 mapSize, GamePack package)
			{
			   
				proxy.Level = LevelRep.CreateNewLevel(name,
													  description,
													  thumbnailPath,
													  pluginPath,
													  mapSize,
													  package);
				
				MenuUIManager.MenuController.StartLoadingLevelForEditing(proxy.Level);
			}

			public void SimulateEditExistingLevel()
			{
				MenuUIManager.MenuController.StartLoadingLevelForEditing(proxy.Level);
			}

			public void SimulateBackButtonPress()
			{
				MenuUIManager.SwitchBack();
			}
#endif
		}

		public LevelRep Level { get; set; }

		protected override ScreenBase ScreenInstance {
			get => screen;
			set => screen = (Screen)value;
		}

		Screen screen;

		public LevelCreationScreen(MenuUIManager menuUIManager)
			:base(menuUIManager)
		{ }

		public override void ExecuteAction(MenuScreenAction action)
		{
			if (action is LevelCreationScreenAction myAction) {
				switch (myAction.Action) {
					case LevelCreationScreenAction.Actions.Edit:
						if (Level == null) {
							screen.SimulateEditNewLevel(myAction.LevelName,
														myAction.Description,
														myAction.ThumbnailPath,
														myAction.PluginPath,
														myAction.MapSize,
														PackageManager.Instance.ActivePackage);
						}
						else {
							//TODO: Simulate changing values - needs changes to the Action class too
							screen.SimulateEditExistingLevel();
						}
						break;
					case LevelCreationScreenAction.Actions.Back:
						screen.SimulateBackButtonPress();
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			else {
				throw new ArgumentException("Action does not belong to the current screen", nameof(action));
			}
		}

		public override void Show()
		{
			if (ScreenInstance != null) {
				return;
			}

			ScreenInstance = new Screen(this);
		}

		public override void Hide()
		{
			if (ScreenInstance == null) {
				return;
			}

			Level = null;
			base.Hide();
		}
	}
}
