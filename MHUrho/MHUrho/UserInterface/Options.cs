using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.StartupManagement;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	

	class Options : MenuScreen {


		enum WindowTypeEnum {
			Windowed = 0,
			BorderlessWindowed = 1,
			Fullscreen = 2
		}



		public override bool Visible {
			get => window.Visible;
			set => window.Visible = value;
		}

		readonly Window window;

		

		//NOTE: Maybe change this to proper variables
		Slider UnitDrawDistance => (Slider)window.GetChild("UnitDrawDistanceSlider", true);

		Slider ProjectileDrawDistance => (Slider) window.GetChild("ProjectileDrawDistanceSlider", true);

		Slider TerrainDrawDistance => (Slider) window.GetChild("TerrainDrawDistanceSlider", true);

		DropDownList Resolutions => (DropDownList) window.GetChild("Resolution", true);

		DropDownList WindowTypes => (DropDownList) window.GetChild("WindowType", true);

		CheckBox HighDPI => (CheckBox) window.GetChild("HighDPI", true);

		CheckBox TripleBuffer => (CheckBox)window.GetChild("TripleBuffer", true);

		CheckBox VSync => (CheckBox)window.GetChild("VSync", true);

		CheckBox DebugHUD => (CheckBox)window.GetChild("DebugHUD", true);

		LineEdit MultiSample => (LineEdit) window.GetChild("MultiSample", true);

		LineEdit RefreshRate => (LineEdit) window.GetChild("RefreshRate", true);

		bool changed;

		public Options(MyGame game, MenuUIManager menuUIManager)
			: base(game, menuUIManager)
		{
			this.Game = game;

			UI.LoadLayoutToElement(UI.Root, game.ResourceCache, "UI/OptionsLayout.xml");

			window = (Window)UI.Root.GetChild("Options");
			window.Visible = false;

			((Button)window.GetChild("Save", true)).Released += SaveButton_Released;
			((Button)window.GetChild("Exit", true)).Released += BackButton_Released;

			InitializeOptions();
		}

		public override void Show()
		{
			SetValues(Game.Config);
			changed = false;
			Visible = true;
		}

		public override void Hide()
		{
			Visible = false;
		}

		void InitializeOptions()
		{
			LinkSliders();

			FillResolutions();
			FillWindowTypes();

			UnitDrawDistance.SliderChanged += (args) => { Game.Config.UnitDrawDistance = args.Value; changed = true; };
			ProjectileDrawDistance.SliderChanged += (args) => {
														Game.Config.ProjectileDrawDistance = args.Value;
														changed = true;
													};

			TerrainDrawDistance.SliderChanged += (args) => {
													Game.Config.TerrainDrawDistance = args.Value;
													changed = true;
												};

			Resolutions.ItemSelected += (args) => {
											Game.Config.Resolution = Game.Config.SupportedResolutions[args.Selection];
											changed = true;
										};

			WindowTypes.ItemSelected += (args) => {
											//Unknown value defaults to Windowed
											Game.Config.Borderless = WindowTypeToBorderless((WindowTypeEnum) args.Selection);
											Game.Config.Fullscreen = WindowTypeToFullscreen((WindowTypeEnum) args.Selection);
											changed = true;
										};

			HighDPI.Toggled += (args) => {
									Game.Config.HighDPI = args.State;
									changed = true;
								};

			TripleBuffer.Toggled += (args) => {
										Game.Config.TripleBuffer = args.State;
										changed = true;
									};

			VSync.Toggled += (args) => {
								Game.Config.VSync = args.State;
								changed = true;
							};

			DebugHUD.Toggled += (args) => {
									Game.Config.DebugHUD = args.State;
									changed = true;
								};

			//Initializes values
			SetValues(Game.Config);
		}

		/// <summary>
		/// Links sliders with their LineEdits
		/// </summary>
		void LinkSliders()
		{
			var sliders = new List<string>
						{
							"UnitDrawDistance",
							"ProjectileDrawDistance",
							"TerrainDrawDistance"
						};

			foreach (var name in sliders) {
				Slider slider = (Slider)window.GetChild(name + "Slider", true);
				LineEdit edit = (LineEdit)window.GetChild(name + "Edit", true);

				slider.SliderChanged += (args) => { edit.Text = ((int)args.Value).ToString(); changed = true; };

				edit.TextChanged += (args) => {
					//TODO: Read max and min values for this slider
					if ((!int.TryParse(args.Text, out int value) || value < 0 || 100 < value) && args.Text != "") {
						((LineEdit)args.Element).Text = ((int)slider.Value).ToString();
					}
					changed = true;

				};

				edit.TextFinished += (args) => {
					//TODO: Read max and min values for this slider
					if (!int.TryParse(args.Text, out int value) || value < 0 || 100 < value) {
						if (args.Text == "") {
							slider.Value = 0;
							((LineEdit)args.Element).Text = 0.ToString();
						}
						else {
							((LineEdit)args.Element).Text = ((int)slider.Value).ToString();
						}
					}
					else {
						slider.Value = value;
					}
					changed = true;
				};

			}

		}

		void FillResolutions()
		{
			var resolutionsElement = Resolutions;

			foreach (var resolution in Game.Config.SupportedResolutions) {
				Text text = new Text {
					Value = resolution.ToString()
				};


				resolutionsElement.AddItem(text);

				//TODO: Text style
				text.SetStyleAuto();
			}

		}

		void FillWindowTypes()
		{
			var windowTypes = new List<string>();
			windowTypes.Insert((int) WindowTypeEnum.Windowed, WindowTypeToString(WindowTypeEnum.Windowed));
			windowTypes.Insert((int)WindowTypeEnum.BorderlessWindowed, WindowTypeToString(WindowTypeEnum.BorderlessWindowed));
			windowTypes.Insert((int)WindowTypeEnum.Fullscreen, WindowTypeToString(WindowTypeEnum.Fullscreen));

			var windowTypesElement = WindowTypes;

			foreach (var type in windowTypes) {
				Text text = new Text {
					Value = type
				};


				windowTypesElement.AddItem(text);

				//TODO: Text style
				text.SetStyleAuto();
			}
		}

		void SaveButton_Released(ReleasedEventArgs args)
		{
			if (changed) {
				Game.Config.SetGraphicsMode(Game.Graphics);
				MenuUIManager.PopUpConfirmation.RequestConfirmation("Save options",
																	"Do you wish to save these settings ?",
																	SaveConfirmation,
																	TimeSpan.FromSeconds(10));
			}
			else {
				SaveConfirmation(true);
			}
			
		}

		void BackButton_Released(ReleasedEventArgs args)
		{
			if (changed) {
				MenuUIManager.PopUpConfirmation.RequestConfirmation("Exit options",
																	"Do you wish to revert these settings to their previous state?",
																	ExitConfirmation);
			}
			else {
				ExitConfirmation(true);
			}
		}

		void ExitConfirmation(bool confirmed)
		{
			if (confirmed) {
				if (changed) {
					Game.Config.Reload();
					SetValues(Game.Config);
				}
				changed = false;
				MenuUIManager.SwitchBack();
			}
		}

		void SaveConfirmation(bool confirmed)
		{
			if (confirmed) {
				if (changed) {
					Game.Config.Save();
					changed = false;
				}
				MenuUIManager.SwitchBack();
			}
			else {
				if (changed) {
					Game.Config.Reload();
					SetValues(Game.Config);
					Game.Config.SetGraphicsMode(Game.Graphics);
					changed = false;
				}
			}
		}

		void SetValues(AppOptions options)
		{
			UnitDrawDistance.Range = options.MaxDrawDistance - options.MinDrawDistance;
			UnitDrawDistance.Value = options.UnitDrawDistance;

			ProjectileDrawDistance.Range = options.MaxDrawDistance - options.MinDrawDistance;
			ProjectileDrawDistance.Value = options.ProjectileDrawDistance;

			TerrainDrawDistance.Range = options.MaxDrawDistance - options.MinDrawDistance;
			TerrainDrawDistance.Value = options.TerrainDrawDistance;

			Resolutions.Selection = (uint)Game.Config.SupportedResolutions.IndexOf(Game.Config.Resolution);
			
			WindowTypes.Selection = (uint)FullscreenAndBorderlessToWindowType(options.Fullscreen, options.Borderless);

			HighDPI.Checked = options.HighDPI;

			TripleBuffer.Checked = options.TripleBuffer;

			VSync.Checked = options.VSync;

			DebugHUD.Checked = options.DebugHUD;

			MultiSample.Text = options.Multisample.ToString();

			RefreshRate.Text = options.RefreshRateCap.ToString();
		}

		string WindowTypeToString(WindowTypeEnum windowType)
		{
			switch (windowType) {
				case WindowTypeEnum.Windowed:
					return "Windowed";
				case WindowTypeEnum.BorderlessWindowed:
					return "Borderless windowed";
				case WindowTypeEnum.Fullscreen:
					return "Fullscreen";
				default:
					throw new ArgumentOutOfRangeException(nameof(windowType), windowType, null);
			}
		}

		bool WindowTypeToFullscreen(WindowTypeEnum windowType)
		{
			return windowType == WindowTypeEnum.Fullscreen;
		}

		bool WindowTypeToBorderless(WindowTypeEnum windowType)
		{
			return windowType == WindowTypeEnum.BorderlessWindowed;
		}

		WindowTypeEnum FullscreenAndBorderlessToWindowType(bool fullscreen, bool borderless)
		{
			if (!fullscreen && !borderless) {
				return WindowTypeEnum.Windowed;
			}

			if (!fullscreen && borderless) {
				return WindowTypeEnum.BorderlessWindowed;
			}

			if (fullscreen && !borderless) {
				return WindowTypeEnum.Fullscreen;
			}

			throw new ArgumentOutOfRangeException(nameof(fullscreen) + " and " + nameof(borderless),
												"Borderless fullscrean is invalid combination");
		}
	}
}
