﻿using System;
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

			class PlayerItem : UIElement {

				public PlayerType ChosenType => playerTypeList.SelectedItem != null ? elementToTypeMap[playerTypeList.SelectedItem] : null;

				public int ChosenTeam => teamList.SelectedItem == null ? elementToTeamMap[teamList.SelectedItem] : 0;

				readonly Dictionary<UIElement, PlayerType> elementToTypeMap;
				readonly Dictionary<UIElement, int> elementToTeamMap;

				readonly DropDownList playerTypeList;
				readonly DropDownList teamList;

				protected PlayerItem(Screen screen, PlayerTypeCategory playerTypeCategory)
				{
					elementToTypeMap = new Dictionary<UIElement, PlayerType>();
					elementToTeamMap = new Dictionary<UIElement, int>();

					var child =
						screen.Game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/PlayerItemLayout.xml"));

					AddChild(child);

					playerTypeList = (DropDownList) child.GetChild("PlayerTypeList", true);
					teamList = (DropDownList) child.GetChild("TeamList", true);

					foreach (var player in screen.Level.GamePack.GetPlayersWithTypeCategory(playerTypeCategory)) {
						var item = InitTypeItem(player, screen.Game, screen.MenuUIManager);
						playerTypeList.AddItem(item);
						elementToTypeMap.Add(item, player);
					}

					for (int teamID = 1; teamID <= screen.Level.MaxNumberOfPlayers; teamID++) {
						UIElement item = InitTeamItem(teamID, screen.Game, screen.MenuUIManager);
						teamList.AddItem(item);
						elementToTeamMap.Add(item, teamID);
					}
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


				static UIElement InitTypeItem(PlayerType player, MyGame game, MenuUIManager menuUIManager)
				{
					var newElement = game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/PlayerTypeItemLayout.xml"),
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
					var newElement = game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/TeamListItemLayout.xml"),
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



			public Screen(LevelSettingsScreen proxy)
				:base(proxy)
			{
				this.proxy = proxy;
				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/LevelSettingsScreenLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild("LevelSettingsWindow");

				customSettingsWindow = (Window)window.GetChild("CustomSettings", true);

				UIElement descriptionTextElement = Game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/DescriptionTextLayout.xml"),
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
				PlayerItem item = ((PlayerItem) playerList.GetItem(0));
				players.SetNeutralPlayer(item.ChosenType);
				item = ((PlayerItem) playerList.GetItem(1));
				players.SetPlayerWithInput(item.ChosenType, item.ChosenTeam);
				for (uint i = 2; i < playerList.NumItems; i++) {
					item = (PlayerItem)playerList.GetItem(i);
					players.AddAIPlayer(item.ChosenType, item.ChosenTeam);
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
				descriptionScrollView.Dispose();
				descriptionText.Dispose();
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
