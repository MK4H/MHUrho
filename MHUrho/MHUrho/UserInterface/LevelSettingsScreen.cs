using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MHUrho.EntityInfo;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.StartupManagement;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class LevelSettingsScreen : MenuScreen
	{
		class Screen :  ScreenBase {

			class PlayerItem : UIElement {

				public PlayerType ChosenType => playerTypeList.SelectedItem != null ? elementToTypeMap[playerTypeList.SelectedItem] : null;

				public int ChosenTeam => teamList.SelectedItem != null ? elementToTeamMap[teamList.SelectedItem] : 0;

				public PlayerInsignia Insignia { get; private set; }

				readonly Dictionary<UIElement, PlayerType> elementToTypeMap;
				readonly Dictionary<UIElement, int> elementToTeamMap;

				readonly DropDownList playerTypeList;
				readonly DropDownList teamList;

				protected PlayerItem(Screen screen, PlayerInsignia insignia, int initialTeamID, PlayerTypeCategory playerTypeCategory)
				{
					this.Insignia = insignia;

					elementToTypeMap = new Dictionary<UIElement, PlayerType>();
					elementToTeamMap = new Dictionary<UIElement, int>();

					var child =
						screen.Game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/PlayerItemLayout.xml", true));

					AddChild(child);

					BorderImage playerShield = (BorderImage) child.GetChild("PlayerShield", true);
					playerTypeList = (DropDownList) child.GetChild("PlayerTypeList", true);
					teamList = (DropDownList) child.GetChild("TeamList", true);

					playerShield.Texture = insignia.ShieldTexture;
					playerShield.ImageRect = insignia.ShieldRectangle;

					foreach (var player in screen.Level.GamePack.GetPlayersWithTypeCategory(playerTypeCategory)) {
						var item = InitTypeItem(player, screen.Game, screen.MenuUIManager);
						playerTypeList.AddItem(item);
						elementToTypeMap.Add(item, player);
					}

					if (playerTypeCategory != PlayerTypeCategory.Neutral) {
						for (int teamID = 1; teamID <= screen.Level.MaxNumberOfPlayers; teamID++)
						{
							UIElement item = InitTeamItem(teamID, screen.Game, screen.MenuUIManager);
							teamList.AddItem(item);
							elementToTeamMap.Add(item, teamID);

							if (teamID == initialTeamID) {
								teamList.Selection = teamList.NumItems - 1;
							}
						}
					}
					else {
						teamList.Visible = false;
					}
				}

				public static PlayerItem CreateAndAddToList(ListView list,
															Screen screen,
															PlayerInsignia insignia,
															int initialTeamID,
															PlayerTypeCategory playerTypeCategory)
				{
					var newItem = new PlayerItem(screen, insignia, initialTeamID, playerTypeCategory);
					list.AddItem(newItem);
					newItem.SetStyle("PlayerItem");
					return newItem;
				}


				static UIElement InitTypeItem(PlayerType player, MyGame game, MenuUIManager menuUIManager)
				{
					var newElement = game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/PlayerTypeItemLayout.xml", true),
														menuUIManager.MenuRoot.GetDefaultStyle());

					BorderImage playerIcon = (BorderImage)newElement.GetChild("PlayerIcon");
					Text playerName = (Text)newElement.GetChild("PlayerName");
					Text playerDescription = (Text)newElement.GetChild("PlayerDescription", true);

					playerIcon.Texture = player.Package.PlayerIconTexture;
					playerIcon.ImageRect = player.IconRectangle;

					playerName.Value = player.Name;
					//TODO: Description
					playerDescription.Value = "Nothing for now";

					return newElement;
				}

				static UIElement InitTeamItem(int teamID, MyGame game, MenuUIManager menuUIManager)
				{
					var newElement = game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/TeamListItemLayout.xml", true),
														menuUIManager.MenuRoot.GetDefaultStyle());

					Text textElement = (Text)newElement.GetChild("TeamIDText");
					textElement.Value = teamID.ToString();

					return newElement;
				}
			}

			readonly LevelSettingsScreen proxy;

			LevelRep Level => proxy.Level;

			readonly Window window;
			readonly Window customSettingsWindow;
			readonly ScrollView descriptionScrollView;
			readonly Text descriptionText;
			readonly BorderImage mapImage;
			readonly ListView playerList;

			readonly LevelLogicCustomSettings pluginCustomSettings;

			public Screen(LevelSettingsScreen proxy)
				:base(proxy)
			{
				this.proxy = proxy;
				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/LevelSettingsScreenLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild("LevelSettingsWindow");

				customSettingsWindow = (Window)window.GetChild("CustomSettings", true);

				UIElement descriptionTextElement = Game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/DescriptionTextLayout.xml", true),
																	MenuUIManager.MenuRoot.GetDefaultStyle());
				descriptionText = (Text) descriptionTextElement.GetChild("DescriptionText");
				descriptionText.Value = Level.Description;
				descriptionScrollView = (ScrollView) window.GetChild("DescriptionScrollView", true);

				descriptionScrollView.ContentElement = descriptionTextElement;

				mapImage = (BorderImage)window.GetChild("MapImage");
				mapImage.Texture = Level.Thumbnail;
				mapImage.ImageRect = new Urho.IntRect(0, 0, Level.Thumbnail.Width, Level.Thumbnail.Height);

				playerList = (ListView)window.GetChild("PlayerListView");

				((Button)window.GetChild("PlayButton", true)).Released += PlayButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released += BackButtonReleased;

				InsigniaGetter insigniaGetter = new InsigniaGetter();

				PlayerItem.CreateAndAddToList(playerList, 
											this, 
											insigniaGetter.MarkUsed(PlayerInsignia.NeutralPlayerInsignia), 
											0,
											PlayerTypeCategory.Neutral);

				PlayerItem.CreateAndAddToList(playerList,
											this,
											insigniaGetter.GetNextUnusedInsignia(),
											1,
											PlayerTypeCategory.Human);

				for (int i = 0; i < Level.MaxNumberOfPlayers - 1; i++) {
					PlayerItem.CreateAndAddToList(playerList, this, insigniaGetter.GetNextUnusedInsignia(), i + 2, PlayerTypeCategory.AI);
				}

				pluginCustomSettings = Level.LevelLogicType.GetCustomSettings(customSettingsWindow);
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
				((Button)window.GetChild("PlayButton", true)).Released -= PlayButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released -= BackButtonReleased;

				playerList.RemoveAllItems();

				window.RemoveAllChildren();
				window.Remove();

				window.Dispose();
				customSettingsWindow.Dispose();
				descriptionScrollView.Dispose();
				descriptionText.Dispose();
				mapImage.Dispose();
				playerList.Dispose();
			}

			void PlayButtonReleased(ReleasedEventArgs args)
			{
				//TODO: Sanity checks

				PlayerItem neutralPlayerItem = ((PlayerItem) playerList.GetItem(0));


				PlayerItem humanPlayerItem = ((PlayerItem) playerList.GetItem(1));
				List<Tuple<PlayerType, int, PlayerInsignia>> aiPlayers = new List<Tuple<PlayerType, int, PlayerInsignia>>();
				for (uint i = 2; i < playerList.NumItems; i++) {
					PlayerItem item = (PlayerItem)playerList.GetItem(i);
					aiPlayers.Add(Tuple.Create(item.ChosenType, item.ChosenTeam, item.Insignia));
				}

				//Has to be last statement in the method, this instance will be released during execution.
				proxy.Play(neutralPlayerItem.ChosenType,
							Tuple.Create(humanPlayerItem.ChosenType, humanPlayerItem.ChosenTeam, humanPlayerItem.Insignia),
							aiPlayers,
							 pluginCustomSettings);
			}

			void BackButtonReleased(ReleasedEventArgs args)
			{
				MenuUIManager.SwitchBack();
			}

			

			public void SimulateBackButton()
			{
				MenuUIManager.SwitchBack();
			}

			public void SimulatePlayButton(LevelSettingsScreenAction screenAction)
			{
				InsigniaGetter insigniaGetter = new InsigniaGetter();
				insigniaGetter.MarkUsed(PlayerInsignia.NeutralPlayerInsignia);
				PlayerType neutralPlayerType = Level.GamePack.GetPlayerType(screenAction.NeutralPlayerTypeName);

				Tuple<PlayerType, int, PlayerInsignia> humanPlayer =
					Tuple.Create(Level.GamePack.GetPlayerType(screenAction.HumanPlayer.Item1),
								screenAction.HumanPlayer.Item2,
								 insigniaGetter.GetNextUnusedInsignia());


				//Has to be last statement in the method, this instance will be released during the execution
				proxy.Play(neutralPlayerType,
							humanPlayer,
							from aiPlayer in screenAction.AIPlayers
							select Tuple.Create(Level.GamePack.GetPlayerType(aiPlayer.Item1),
												aiPlayer.Item2,
												insigniaGetter.GetNextUnusedInsignia()),
							 pluginCustomSettings);
			}
		}

		public LevelRep Level { get; set; }

		protected override ScreenBase ScreenInstance {
			get => screen;
			set => screen = (Screen)value;
		}

		Screen screen;

		public LevelSettingsScreen(MenuUIManager menuUIManager)
			:base(menuUIManager)
		{

		}

		public override void ExecuteAction(MenuScreenAction action)
		{
			if (action is LevelSettingsScreenAction myAction) {
				switch (myAction.Action)
				{
					case LevelSettingsScreenAction.Actions.Play:
						screen.SimulatePlayButton(myAction);
						break;
					case LevelSettingsScreenAction.Actions.Back:
						screen.SimulateBackButton();
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(action), myAction.Action, "Unknown action type");
				}
			}
			else {
				throw new ArgumentException("Action does not belong to the current screen", nameof(action));
			}
		}

		public override void Show()
		{
			if (screen != null) {
				return;
			}

			if (Level == null) {
				throw new InvalidOperationException($"{nameof(Level)} has to be set before Showing this screen");
			}

			screen = new Screen(this);
		}

		public override void Hide()
		{
			if (screen == null) {
				return;
			}

			Level = null;
			base.Hide();
		}

		/// <summary>
		/// Starts the loading of the <see cref="Level"/> for playing.
		/// Loads the level with neutral player of type <paramref name="neutralPlayerType"/>,
		/// human player of type <paramref name="humanPlayer"/>,
		/// ai players of types <paramref name="aiPlayers"/> and
		/// gives the level plugin the <paramref name="pluginCustomSettings"/> it defined in the provided window.
		/// </summary>
		/// <param name="neutralPlayerType">The type of the neutral player in the loaded level.</param>
		/// <param name="humanPlayer">The type of the human player in the loaded level.</param>
		/// <param name="aiPlayers">The types of ai players in the loaded level.</param>
		/// <param name="pluginCustomSettings">Data the plugin requested from the user.</param>
		void Play(PlayerType neutralPlayerType,
				Tuple<PlayerType, int, PlayerInsignia> humanPlayer,
				IEnumerable<Tuple<PlayerType, int, PlayerInsignia>> aiPlayers,
				LevelLogicCustomSettings pluginCustomSettings)
		{
			PlayerSpecification players = new PlayerSpecification();

			players.SetNeutralPlayer(neutralPlayerType);
			players.SetHumanPlayer(humanPlayer.Item1, humanPlayer.Item2, humanPlayer.Item3);
			foreach (var aiPlayer in aiPlayers)
			{
				players.AddAIPlayer(aiPlayer.Item1, aiPlayer.Item2, aiPlayer.Item3);
			}


			ILevelLoader loader = MenuUIManager.MenuController.GetLevelLoaderForPlaying(Level, players, pluginCustomSettings);
			MenuUIManager.SwitchToLoadingScreen(loader);

			loader.Finished += (progress) => {
									MenuUIManager.Clear();
								};
			loader.Failed += (progress, message) => {
								//Switch back from the loading screen
								MenuUIManager.SwitchBack();
								MenuUIManager.ErrorPopUp.DisplayError("Error", message, this);
							};

			loader.StartLoading();
		}
	}
}
