using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

			class LogicTypeItem : UIElement {

				public LevelLogicType LogicType { get; private set; }

				public LogicTypeItem(Screen screen, LevelLogicType logicType)
				{
					this.LogicType = logicType;
					this.LayoutMode = LayoutMode.Horizontal;
					var newItem = screen.Game.UI.LoadLayout(screen.Game.PackageManager.GetXmlFile("UI/LevelLogicTypeItemLayout.xml", true),
															screen.MenuUIManager.MenuRoot.GetDefaultStyle());

					((Text) newItem.GetChild("LogicTypeText")).Value = logicType.Name;
					AddChild(newItem);
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

			readonly Window window;
			readonly LineEdit nameEdit;
			readonly SliderDuo mapSize;
			readonly DropDownList logicTypeList;
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

				thumbnailPathButton = (Button) window.GetChild("ThumbnailPathButton", true);

				thumbnailPathText = new PathText((Text) thumbnailPathButton.GetChild("ThumbnailPathText"));

				logicTypeList = (DropDownList) window.GetChild("PluginTypeList", true);

				descriptionEdit = (LineEdit) window.GetChild("DescriptionEdit", true);


				nameEdit.TextChanged += NameChanged;
				descriptionEdit.TextChanged += DescriptionChanged;
				thumbnailPathButton.Released += ThumbnailPathButtonReleased;

				((Button)window.GetChild("EditButton", true)).Released += EditButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released += BackButtonReleased;

				FillLogicTypes();

				//Editing existing level, load the values and lock the unchangable ones (levelSize and plugin)
				if (Level != null) {
					nameEdit.Text = Level.Name;
					mapSize.Value = Level.MapSize;
					descriptionEdit.Text = Level.Description;
					thumbnailPathText.Value = Level.ThumbnailPath;
					for (uint i = 0; i < logicTypeList.NumItems; i++) {
						var item = (LogicTypeItem)logicTypeList.GetItem(i);
						if (item.LogicType == Level.LevelLogicType) {
							logicTypeList.Selection = i;
						}
					}


					//Disable the unchangable ones
					mapSize.Enabled = false;
					logicTypeList.Enabled = false;
				}
			}

			public override void EnableInput()
			{
				window.SetDeepEnabled(true);
			}

			public override void DisableInput()
			{
				window.SetDeepEnabled(false);
			}

			public override void ResetInput()
			{
				window.ResetDeepEnabled();
			}

			public override void Dispose()
			{
				nameEdit.TextChanged -= NameChanged;
				descriptionEdit.TextChanged -= DescriptionChanged;
				thumbnailPathButton.Released -= ThumbnailPathButtonReleased;

				((Button)window.GetChild("EditButton", true)).Released -= EditButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released -= BackButtonReleased;

				nameEdit.Dispose();
				mapSize.Dispose();
				logicTypeList.Dispose();
				thumbnailPathButton.Dispose();
				thumbnailPathText.Dispose();
				descriptionEdit.Dispose();

				window.RemoveAllChildren();
				window.Remove();
				window.Dispose();
			}

			async void EditButtonReleased(ReleasedEventArgs args)
			{
				//TODO: SAVE THE CHANGES TO EXISTING LEVEL
				//Creating new level
				if (proxy.Level == null) {
					proxy.Level = LevelRep.CreateNewLevel(Name,
														Description,
														ThumbnailPath,
														((LogicTypeItem)logicTypeList.SelectedItem).LogicType,
														mapSize.Value,
														Game.PackageManager.ActivePackage);
				}
				else {
					//Creates clone with the new or old name
					// if the name was the old one, the old levelRep and the saved level is overwritten
					// if the name is new, new level is created in the game pack
					proxy.Level = proxy.Level.CreateClone(Name,
														Description,
														ThumbnailPath);
					try {
						proxy.Level.GamePack.SaveLevelPrototype(proxy.Level, true);
					}
					catch (Exception e) {
						await MenuUIManager.ErrorPopUp.DisplayError("Error", $"Level cloning failed with: \"{e.Message}\"");
						return;
					}
				}

				//Has to be the last statement in the method, this instance will be released during the execution
				proxy.EditLevel(proxy.Level);
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

			void ThumbnailPathButtonReleased(ReleasedEventArgs args)
			{
				RequestPathToText(thumbnailPathText);
			}

			void FillLogicTypes()
			{
				foreach (var logicType in Game.PackageManager.ActivePackage.LevelLogicTypes) {
					logicTypeList.AddItem(new LogicTypeItem(this, logicType));
				}
			}

			async void RequestPathToText(PathText pathText)
			{
				try {
					string oldText = pathText.Value;

					var result = await MenuUIManager.
										FileBrowsingPopUp.
										Request(Game.PackageManager.ActivePackage.RootedDirectoryPath,
												SelectOption.File,
												 pathText.HasDefaultValue ? null : pathText.Value);
					

					string newText = result == null ? oldText : result.RelativePath;
					await MHUrhoApp.InvokeOnMainSafeAsync(() => pathText.Value = newText);
				}
				catch (OperationCanceledException) {
					//Text should not have changed
				}
			}

			

			public void SimulateBackButtonPress()
			{
				MenuUIManager.SwitchBack();
			}
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
							Level = LevelRep.CreateNewLevel(myAction.LevelName,
															myAction.Description,
															myAction.ThumbnailPath,
															Game.PackageManager.ActivePackage.GetLevelLogicType(myAction.LogicTypeName),
															myAction.MapSize,
															Game.PackageManager.ActivePackage);
	
						}
						else {
							//TODO: Simulate changing values - needs changes to the Action class too
						}
						EditLevel(Level);
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

		//public override void Hide()
		//{
		//	if (ScreenInstance == null) {
		//		return;
		//	}

		//	
		//	Level = null;
		//	base.Hide();
		//}

		/// <summary>
		/// Starts the loading process of the <paramref name="level"/> for editing.
		/// Cannot be implemented in <see cref="Screen"/> because it switches to different screens
		/// during execution, which releases our <see cref="screen"/>.
		/// </summary>
		/// <param name="level">The level to load for editing</param>
		void EditLevel(LevelRep level)
		{
			ILevelLoader loader = MenuUIManager.MenuController.GetLevelLoaderForEditing(level);
			MenuUIManager.SwitchToLoadingScreen(loader);

			loader.Finished += (progress) => {
									MenuUIManager.Clear();
								};
			loader.Failed += (progress, message) => {
								Level?.Dispose();
								Level = null;
								MenuUIManager.SwitchBack();
								MenuUIManager.ErrorPopUp.DisplayError("Error", $"Level loading failed with: \"{message}\"");
							};

			loader.StartLoading();
		}
	}
}
