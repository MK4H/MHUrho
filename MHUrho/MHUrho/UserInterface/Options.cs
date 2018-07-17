using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class Options : MenuScreen {
		public override bool Visible {
			get => window.Visible;
			set => window.Visible = value;
		}

		readonly Window window;


		public Options(MyGame game, MenuUIManager menuUIManager)
			: base(game, menuUIManager)
		{
			this.game = game;

			UI.LoadLayoutToElement(UI.Root, game.ResourceCache, "UI/OptionsLayout.xml");

			window = (Window)UI.Root.GetChild("Options");
			window.Visible = false;

			InitializeOptions();
		}

		void InitializeOptions()
		{
			LinkSliders();

			FillResolutions();
			FillWindowTypes();

			InitializeSlider("UnitDrawDistanceSlider",
							(args) => { game.Config.UnitDrawDistance = args.Value; },
							game.Config.UnitDrawDistance);

			InitializeSlider("ProjectileDrawDistanceSlider",
							(args) => { game.Config.ProjectileDrawDistance = args.Value; },
							game.Config.ProjectileDrawDistance);

			InitializeSlider("TerrainDrawDistanceSlider",
							(args) => { game.Config.TerrainDrawDistance = args.Value; },
							game.Config.TerrainDrawDistance);



			List<IntVector2> resolutions = new List<IntVector2>
											{
												new IntVector2(800,600),
												new IntVector2(1024,768),
												new IntVector2(1920,1080)
											};



			InitializeDropDownList("Resolution",
									(args) => {
										game.Config.Resolution = resolutions[args.Selection];
									},
									(uint)resolutions.IndexOf(game.Config.Resolution));

			InitializeDropDownList("WindowType",
									(args) => {
										switch (args.Selection) {
											case 0: // Windowed
												game.Config.Borderless = false;
												game.Config.Fullscreen = false;

												break;
											case 1: // Borderless windowed
												game.Config.Borderless = true;
												game.Config.Fullscreen = false;
												break;
											case 2: // Fullscreen
												game.Config.Borderless = false;
												game.Config.Fullscreen = true;
												break;
											default:
												throw new ArgumentOutOfRangeException(nameof(args.Selection), "Invalid selection");

										}
									},
									(uint)(game.Config.Fullscreen ? 2 : (game.Config.Borderless ? 1 : 0))
								 );

			InitializeCheckbox("HighDPI",
								(args) => { game.Config.HighDPI = args.State; },
								game.Config.HighDPI);

			InitializeCheckbox("TripleBuffer",
								(args) => { game.Config.TripleBuffer = args.State; },
								game.Config.TripleBuffer);

			InitializeCheckbox("VSync",
								(args) => { game.Config.VSync = args.State; },
								game.Config.VSync);

			InitializeCheckbox("DebugHUD",
								(args) => { game.Config.DebugHUD = args.State; },
								game.Config.DebugHUD);

			//TODO: Let player confirm and save to file
			((Button)window.GetChild("Save", true)).Released += (args) => { game.Config.SetGraphicsMode(game.Graphics); };
			((Button)window.GetChild("Exit", true)).Released += (args) => { MenuUIManager.SwitchBack(); };
		}

		void InitializeSlider(string name, Action<SliderChangedEventArgs> action, float initialValue)
		{
			Slider slider = (Slider)window.GetChild(name, true);
			slider.Value = initialValue;
			slider.SliderChanged += action;
		}

		void InitializeDropDownList(string name, Action<ItemSelectedEventArgs> action, uint initialSelection)
		{
			DropDownList dropDownList = (DropDownList)window.GetChild(name, true);
			dropDownList.ItemSelected += action;
			dropDownList.Selection = initialSelection;
		}

		void InitializeCheckbox(string name, Action<ToggledEventArgs> action, bool boxChecked)
		{
			CheckBox checkbox = (CheckBox)window.GetChild(name, true);
			checkbox.Toggled += action;
			checkbox.Checked = boxChecked;
		}

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

				slider.SliderChanged += (args) => { edit.Text = ((int)args.Value).ToString(); };

				edit.TextChanged += (args) => {
					//TODO: Read max and min values for this slider
					if ((!int.TryParse(args.Text, out int value) || value < 0 || 100 < value) && args.Text != "") {
						((LineEdit)args.Element).Text = ((int)slider.Value).ToString();
					}

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
				};

			}

		}

		void FillResolutions()
		{
			DropDownList list = (DropDownList)window.GetChild("Resolution", true);
			var resolutions = new List<string>
							{
								"800x600",
								"1024x768",
								"1920x1080"
							};
			foreach (var resolution in resolutions) {
				Text text = new Text {
					Value = resolution
				};


				list.AddItem(text);

				text.SetStyleAuto();
			}

		}

		void FillWindowTypes()
		{
			DropDownList list = (DropDownList)window.GetChild("WindowType", true);
			var windowTypes = new List<string>
							{
								"Windowed",
								"Borderless windowed",
								"Fullscreen"
							};
			foreach (var type in windowTypes) {
				Text text = new Text {
					Value = type
				};


				list.AddItem(text);

				text.SetStyleAuto();
			}
		}


		public override void Show()
		{
			Visible = true;
		}

		public override void Hide()
		{
			Visible = false;
		}
	}
}
