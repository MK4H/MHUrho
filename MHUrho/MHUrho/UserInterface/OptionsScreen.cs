using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MHUrho.Helpers;
using MHUrho.StartupManagement;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	

	class OptionsScreen : MenuScreen {

		class Screen : ScreenBase {
			enum WindowTypeEnum {
				Windowed = 0,
				BorderlessWindowed = 1,
				Fullscreen = 2
			}


			readonly OptionsScreen proxy;

			readonly Window window;

			//NOTE: Maybe change this to proper variables
			SliderLineEditCombo UnitDrawDistance { get; set; }

			SliderLineEditCombo ProjectileDrawDistance { get; set; }

			SliderLineEditCombo TerrainDrawDistance { get; set; }

			DropDownList Resolutions => (DropDownList)window.GetChild("Resolution", true);

			DropDownList WindowTypes => (DropDownList)window.GetChild("WindowType", true);

			CheckBox HighDPI => (CheckBox)window.GetChild("HighDPI", true);

			CheckBox TripleBuffer => (CheckBox)window.GetChild("TripleBuffer", true);

			CheckBox VSync => (CheckBox)window.GetChild("VSync", true);

			CheckBox DebugHUD => (CheckBox)window.GetChild("DebugHUD", true);

			LineEdit MultiSample => (LineEdit)window.GetChild("MultiSample", true);

			LineEdit RefreshRate => (LineEdit)window.GetChild("RefreshRate", true);

			SliderLineEditCombo CameraScroll { get; set; }

			SliderLineEditCombo CameraRotation { get; set; }

			SliderLineEditCombo MouseCamRotation { get; set; }

			SliderLineEditCombo ZoomSpeed { get; set; }

			CheckBox BorderMovement => (CheckBox)window.GetChild("BorderMoveCheckBox", true);

			bool changed;

			public Screen(OptionsScreen proxy)
				:base(proxy)
			{
				this.proxy = proxy;

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/OptionsLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild("Options");

				((Button)window.GetChild("Save", true)).Released += SaveButton_Released;
				((Button)window.GetChild("Exit", true)).Released += BackButton_Released;

				InitializeOptions();
			}

			void InitializeOptions()
			{
				InitSliders();

				FillResolutions();
				FillWindowTypes();

				UnitDrawDistance.ValueChanged += UnitDrawDistanceChanged;
				ProjectileDrawDistance.ValueChanged += ProjectileDrawDistanceChanged;
				TerrainDrawDistance.ValueChanged += TerrainDrawDistanceChanged;
				CameraScroll.ValueChanged += CameraScrollChanged;
				CameraRotation.ValueChanged += CameraRotationChanged;
				MouseCamRotation.ValueChanged += MouseCamRotationChanged;
				ZoomSpeed.ValueChanged += ZoomSpeedChanged;
				Resolutions.ItemSelected += ResolutionSelected;
				WindowTypes.ItemSelected += WindowTypeSelected;
				HighDPI.Toggled += HighDPIToggled;
				TripleBuffer.Toggled += TripleBufferToggled;
				VSync.Toggled += VSyncToggled;
				DebugHUD.Toggled += DebugHUDToggled;
				BorderMovement.Toggled += BorderMovementToggled;

				//Initializes values
				SetValues(Game.Config);
			}

			public override void Dispose()
			{
				UnitDrawDistance.ValueChanged -= UnitDrawDistanceChanged;
				ProjectileDrawDistance.ValueChanged -= ProjectileDrawDistanceChanged;
				TerrainDrawDistance.ValueChanged -= TerrainDrawDistanceChanged;
				CameraScroll.ValueChanged -= CameraScrollChanged;
				CameraRotation.ValueChanged -= CameraRotationChanged;
				MouseCamRotation.ValueChanged -= MouseCamRotationChanged;
				ZoomSpeed.ValueChanged -= ZoomSpeedChanged;
				Resolutions.ItemSelected -= ResolutionSelected;
				WindowTypes.ItemSelected -= WindowTypeSelected;
				HighDPI.Toggled -= HighDPIToggled;
				TripleBuffer.Toggled -= TripleBufferToggled;
				VSync.Toggled -= VSyncToggled;
				DebugHUD.Toggled -= DebugHUDToggled;
				BorderMovement.Toggled -= BorderMovementToggled;

				UnitDrawDistance.Dispose();
				ProjectileDrawDistance.Dispose();
				TerrainDrawDistance.Dispose();

				CameraScroll.Dispose();
				CameraRotation.Dispose();
				MouseCamRotation.Dispose();
				ZoomSpeed.Dispose();

				window.RemoveAllChildren();
				window.Remove();
				window.Dispose();
			}

			void InitSliders()
			{
				UnitDrawDistance = new ValueSlLeCombo((Slider)window.GetChild("UnitDrawDistanceSlider", true),
													(LineEdit)window.GetChild("UnitDrawDistanceEdit", true),
													Game.Config.MinDrawDistance,
													Game.Config.MaxDrawDistance);

				ProjectileDrawDistance = new ValueSlLeCombo((Slider)window.GetChild("ProjectileDrawDistanceSlider", true),
															(LineEdit)window.GetChild("ProjectileDrawDistanceEdit", true),
															Game.Config.MinDrawDistance,
															Game.Config.MaxDrawDistance);

				TerrainDrawDistance = new ValueSlLeCombo((Slider)window.GetChild("TerrainDrawDistanceSlider", true),
														(LineEdit)window.GetChild("TerrainDrawDistanceEdit", true),
														Game.Config.MinDrawDistance,
														Game.Config.MaxDrawDistance);

				CameraScroll = new PercentageSlLeCombo((Slider)window.GetChild("KbCamScrollSlider", true),
													 (LineEdit)window.GetChild("KbCamScrollEdit", true),
													 () => Game.Config.MaxCameraScrollSensitivity);

				CameraRotation = new PercentageSlLeCombo((Slider)window.GetChild("KbRotSlider", true),
														(LineEdit)window.GetChild("KbRotEdit", true),
														 () => Game.Config.MaxCameraRotationSensitivity);

				MouseCamRotation = new PercentageSlLeCombo((Slider)window.GetChild("MsRotSlider", true),
															(LineEdit)window.GetChild("MsRotEdit", true),
														 () => Game.Config.MaxMouseRotationSensitivity);

				ZoomSpeed = new PercentageSlLeCombo((Slider)window.GetChild("ZoomSlider", true),
													(LineEdit)window.GetChild("ZoomEdit", true),
													() => Game.Config.MaxZoomSensitivity);
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
				windowTypes.Insert((int)WindowTypeEnum.Windowed, WindowTypeToString(WindowTypeEnum.Windowed));
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
					MenuUIManager.ConfirmationPopUp.RequestConfirmation("Save config",
																		"Do you wish to save these settings ?",
																		TimeSpan.FromSeconds(10)).ContinueWith(SaveConfirmation);
				}
				else {
					SaveConfirmation(true);
				}

			}

			void BackButton_Released(ReleasedEventArgs args)
			{
				if (changed) {
					MenuUIManager.ConfirmationPopUp.RequestConfirmation("Exit config",
																		"Do you wish to revert these settings to their previous state?").ContinueWith(ExitConfirmation);
				}
				else {
					ExitConfirmation(true);
				}
			}

			void ExitConfirmation(Task<bool> confirmed)
			{
				ExitConfirmation(confirmed.Result);
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

			void SaveConfirmation(Task<bool> confirmed)
			{
				SaveConfirmation(confirmed.Result);
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

			void SetValues(AppConfig config)
			{
				UnitDrawDistance.Value = config.UnitDrawDistance;

				ProjectileDrawDistance.Value = config.ProjectileDrawDistance;

				TerrainDrawDistance.Value = config.TerrainDrawDistance;

				Resolutions.Selection = (uint)Game.Config.SupportedResolutions.IndexOf(Game.Config.Resolution);

				WindowTypes.Selection = (uint)FullscreenAndBorderlessToWindowType(config.Fullscreen, config.Borderless);

				HighDPI.Checked = config.HighDPI;

				TripleBuffer.Checked = config.TripleBuffer;

				VSync.Checked = config.VSync;

				DebugHUD.Checked = config.DebugHUD;

				MultiSample.Text = config.Multisample.ToString();

				RefreshRate.Text = config.RefreshRateCap.ToString();

				CameraScroll.Value = config.CameraScrollSensitivity;

				CameraRotation.Value = config.CameraRotationSensitivity;

				MouseCamRotation.Value = config.MouseRotationSensitivity;

				ZoomSpeed.Value = config.ZoomSensitivity;

				BorderMovement.Checked = config.MouseBorderCameraMovement;
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


			void ResolutionSelected(ItemSelectedEventArgs args){
				Game.Config.Resolution = Game.Config.SupportedResolutions[args.Selection];
				changed = true;
			}

			void WindowTypeSelected(ItemSelectedEventArgs args) {
				//Unknown value defaults to Windowed
				Game.Config.Borderless = WindowTypeToBorderless((WindowTypeEnum) args.Selection);
				Game.Config.Fullscreen = WindowTypeToFullscreen((WindowTypeEnum) args.Selection);
				changed = true;
			}

			void HighDPIToggled(ToggledEventArgs args)
			{
				Game.Config.HighDPI = args.State;
				changed = true;
			}

			void TripleBufferToggled( ToggledEventArgs args) {
				Game.Config.TripleBuffer = args.State;
				changed = true;
			}

			void VSyncToggled(ToggledEventArgs args) {
				Game.Config.VSync = args.State;
				changed = true;
			}

			void DebugHUDToggled(ToggledEventArgs args) {
				Game.Config.DebugHUD = args.State;
				changed = true;
			}

			void BorderMovementToggled(ToggledEventArgs args) {
				Game.Config.MouseBorderCameraMovement = args.State;
				changed = true;
			}

			void UnitDrawDistanceChanged(float newValue) {
				Game.Config.UnitDrawDistance = newValue;
				changed = true;
			}

			void ProjectileDrawDistanceChanged(float newValue) {
				Game.Config.ProjectileDrawDistance = newValue;
				changed = true;
			}

			void TerrainDrawDistanceChanged(float newValue) {
				Game.Config.TerrainDrawDistance = newValue;
				changed = true;
			}

			void CameraScrollChanged(float newValue) {
				Game.Config.CameraScrollSensitivity = newValue;
				changed = true;
			}

			void CameraRotationChanged(float newValue) {
				Game.Config.CameraRotationSensitivity = newValue;
				changed = true;
			}

			void MouseCamRotationChanged(float newValue) {
				Game.Config.MouseRotationSensitivity = newValue;
				changed = true;
			}
	
			void ZoomSpeedChanged(float newValue) {
				Game.Config.ZoomSensitivity = newValue;
				changed = true;
			}
		}

		protected override ScreenBase ScreenInstance {
			get => screen;
			set => screen = (Screen)value;
		}


		Screen screen;

		public OptionsScreen(MenuUIManager menuUIManager)
			:base(menuUIManager)
		{
		}

		public override void ExecuteAction(MenuScreenAction action)
		{
			throw new NotImplementedException();
		}

		public override void Show()
		{
			if (screen != null) {
				return;
			}

			screen = new Screen(this);
		}

	}
}
