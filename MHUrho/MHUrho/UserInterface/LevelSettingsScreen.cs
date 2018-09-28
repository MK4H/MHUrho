using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.StartupManagement;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class LevelSettingsScreen : MenuScreen
	{
		class Screen :  ScreenBase {

			class PlayerItem : DropDownList {

				public PlayerType ChosenType => SelectedItem != null ? elementToTypeMap[SelectedItem] : null; 

				readonly Dictionary<UIElement, PlayerType> elementToTypeMap;

				protected PlayerItem(Screen screen, PlayerTypeCategory playerTypeCategory)
				{
					elementToTypeMap = new Dictionary<UIElement, PlayerType>();

					foreach (var player in screen.Level.GamePack.GetPlayersWithTypeCategory(playerTypeCategory)) {
						var item = InitializeItem(player, screen.Game, screen.MenuUIManager);
						this.AddItem(item);
						elementToTypeMap.Add(item, player);
					}

					PlaceholderText = "Empty player slot";
				}

				public static PlayerItem CreateAndAddToList(ListView list,
															Screen screen,
															PlayerTypeCategory playerTypeCategory)
				{
					var newItem = new PlayerItem(screen, playerTypeCategory);
					list.AddItem(newItem);
					newItem.SetStyle("PlayerItem");
					return newItem;
				}

				static UIElement InitializeItem(PlayerType player, MyGame game, MenuUIManager menuUIManager)
				{
					var newElement = game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/PlayerListViewItemLayout.xml"),
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
			}

			readonly LevelSettingsScreen proxy;

			LevelRep Level => proxy.Level;

			readonly Window window;
			readonly Window customSettingsWindow;
			readonly ScrollView descriptionView;
			readonly BorderImage mapImage;
			readonly ListView playerList;



			public Screen(LevelSettingsScreen proxy)
				:base(proxy)
			{
				this.proxy = proxy;
				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/LevelSettingsScreenLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild("LevelSettingsWindow");

				customSettingsWindow = (Window)window.GetChild("CustomSettings");

				descriptionView = (ScrollView)window.GetChild("DescriptionScrollView");

				mapImage = (BorderImage)window.GetChild("MapImage");

				playerList = (ListView)window.GetChild("PlayerListView");

				((Button)window.GetChild("PlayButton", true)).Released += PlayButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released += BackButtonReleased;

				PlayerItem.CreateAndAddToList(playerList, this, PlayerTypeCategory.Neutral);
				PlayerItem.CreateAndAddToList(playerList, this, PlayerTypeCategory.Human);

				for (int i = 0; i < Level.MaxNumberOfPlayers - 1; i++) {
					PlayerItem.CreateAndAddToList(playerList, this, PlayerTypeCategory.AI);
				}
			}

			void PlayButtonReleased(ReleasedEventArgs args)
			{
				//TODO: Sanity checks
				PlayerSpecification players = new PlayerSpecification();
				players.SetNeutralPlayer(((PlayerItem) playerList.GetItem(0)).ChosenType);
				players.SetPlayerWithInput(((PlayerItem)playerList.GetItem(1)).ChosenType);
				for (uint i = 2; i < playerList.NumItems; i++) {
					var item = (PlayerItem)playerList.GetItem(i);
					players.AddAIPlayer(item.ChosenType);
				}
				MenuUIManager.MenuController.StartLoadingLevelForPlaying(Level, players);
			}

			void BackButtonReleased(ReleasedEventArgs args)
			{
				MenuUIManager.SwitchBack();
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
				descriptionView.Dispose();
				mapImage.Dispose();
				playerList.Dispose();				
			}

		}

		//TODO: Ensure that Show cannot be called with Level null, that level is not changed after show etc.
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
			throw new NotImplementedException();
		}

		public override void Show()
		{
			if (screen != null) {
				return;
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

		
	}
}
