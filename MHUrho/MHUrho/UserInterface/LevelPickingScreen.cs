﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.StartupManagement;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.UserInterface
{
	class LevelPickingScreen : MenuScreen
	{

		class Screen : ScreenBase {

			const string DeleteButtonName = "DeleteButton";
			const string EditButtonName = "EditButton";
			const string PlayButtonName = "PlayButton";
			const string BackButtonName = "BackButton";

			const string newLevelItemTexturePath = "Textures/NewLevelItem.png";

			readonly LevelPickingScreen proxy;

			readonly Window window;
			readonly ListView listView;
			readonly Button deleteButton;
			readonly Button editButton;
			readonly Button playButton;

			
			readonly Texture2D newLevelItemTexture;

			readonly List<LevelPickingItem> items;

			GamePack Package => proxy.Package;

			public Screen(LevelPickingScreen proxy)
				:base(proxy)
			{
				this.proxy = proxy;

				items = new List<LevelPickingItem>();

				newLevelItemTexture = PackageManager.Instance.GetTexture2D(newLevelItemTexturePath);

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/LevelPickingLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild("LevelPickingWindow");

				listView = (ListView)window.GetChild("ListView");

				deleteButton = (Button) window.GetChild(DeleteButtonName, true);
				editButton = (Button)window.GetChild(EditButtonName, true);
				playButton = (Button)window.GetChild(PlayButtonName, true);

				deleteButton.Released += LevelManipulatingButtonPressed;
				editButton.Released += LevelManipulatingButtonPressed;
				playButton.Released += LevelManipulatingButtonPressed;
				((Button)window.GetChild(BackButtonName, true)).Released += BackButtonReleased;

				playButton.Enabled = false;
				deleteButton.Enabled = false;
				editButton.Enabled = false;

				GetLevels(listView);
			}

			public override void Dispose()
			{
				deleteButton.Released -= LevelManipulatingButtonPressed;
				editButton.Released -= LevelManipulatingButtonPressed;
				playButton.Released -= LevelManipulatingButtonPressed;
				((Button)window.GetChild("BackButton", true)).Released -= BackButtonReleased;

				foreach (var item in items) {
					item.Dispose();
				}

				window.RemoveAllChildren();
				window.Remove();

				window.Dispose();
				listView.Dispose();
				deleteButton.Dispose();
				editButton.Dispose();
				playButton.Dispose();
			}

			void GetLevels(ListView listView)
			{
				AddItem(new LevelPickingNewLevelItem(Game, newLevelItemTexture, "Create New Level"), listView);
				
				foreach (var level in proxy.Package.Levels) {
					AddItem(new LevelPickingLevelItem(level, Game), listView);
				}
			}

			void LevelManipulatingButtonPressed(ReleasedEventArgs args)
			{
				foreach (var item in items)
				{
					if (item.IsSelected)
					{
						switch (args.Element.Name) {
							case DeleteButtonName:
								DeleteLevel(item);
								break;
							case EditButtonName:
								EditLevel(item);
								break;
							case PlayButtonName:
								PlayLevel(item);
								break;
							default:
								throw new ArgumentOutOfRangeException(nameof(args), args.Element.Name, "Unknown button pressed");
						}
						return;
					}
				}
			}

			void DeleteLevel(LevelPickingItem item)
			{
				if (item is LevelPickingLevelItem levelItem) {
					item.Deselect();
					Package.RemoveLevel(levelItem.Level);
					listView.RemoveItem(item.Element);
					items.Remove(item);
					listView.UpdateInternalLayout();
				}
				else {
					throw new InvalidOperationException("Delete button was pressed when it should not have been possible to press");
				}
			}

			void EditLevel(LevelPickingItem item)
			{
				if (item is LevelPickingLevelItem levelItem) {
					SwitchToEditingExistingLevel(levelItem.Level);
					return;
				}
				else if (item is LevelPickingNewLevelItem newItem) {
					SwitchToEditingNewLevel();
					return;
				}
				else {
					throw new InvalidOperationException("Edit button was pressed when it should not have been possible to press");
				}
			}

			void PlayLevel(LevelPickingItem item)
			{
				if (item is LevelPickingLevelItem levelItem)
				{
					SwitchToPlayingLevel(levelItem.Level);
					return;
				}
				else
				{
					throw new InvalidOperationException("Play button was pressed when it should not have been possible to press");
				}
			}

			void BackButtonReleased(ReleasedEventArgs args)
			{
				MenuUIManager.SwitchBack();
			}

			void ItemSelected(LevelPickingItem selectedItem)
			{
				listView.Selection = listView.FindItem(selectedItem.Element);

				foreach (var item in items) {
					if (selectedItem != item) {
						item.Deselect();
					}
				}

				playButton.Enabled = selectedItem is LevelPickingLevelItem;
				deleteButton.Enabled = playButton.Enabled;
				editButton.Enabled = true;
			}

			void ItemDeselected(LevelPickingItem deselectedItem)
			{
				if (listView.SelectedItem == deselectedItem.Element) {
					playButton.Enabled = false;
					deleteButton.Enabled = false;
					editButton.Enabled = false;

					listView.ClearSelection();
				}
			}

			void AddItem(LevelPickingItem newItem, ListView listView)
			{
				items.Add(newItem);
				newItem.Selected += ItemSelected;
				newItem.Deselected += ItemDeselected;
				listView.AddItem(newItem.Element);
			}

			void SwitchToEditingExistingLevel(LevelRep level)
			{
				MenuUIManager.SwitchToLevelCreationScreen(level);
			}

			void SwitchToEditingNewLevel()
			{
				MenuUIManager.SwitchToLevelCreationScreen(null);
			}

			void SwitchToPlayingLevel(LevelRep level)
			{
				MenuUIManager.SwitchToLevelSettingsScreen(level);
			}
#if DEBUG
			public void SimulateEditPickingLevel(string levelName)
			{
				foreach (var item in items) {
					if (item is LevelPickingLevelItem levelItem && levelItem.Level.Name == levelName)
					{
						SwitchToEditingExistingLevel(levelItem.Level);
						return;
					}
				}

				throw new ArgumentOutOfRangeException(nameof(levelName),
													  levelName,
													  "Level with this name does not exist");
			}

			public void SimulateEditPickingNewLevel()
			{
				SwitchToEditingNewLevel();
			}

			public void SimulatePlayPickingLevel(string levelName)
			{
				foreach (var item in items)
				{
					if (item is LevelPickingLevelItem levelItem && levelItem.Level.Name == levelName)
					{
						SwitchToPlayingLevel(levelItem.Level);
						return;
					}
				}

				throw new ArgumentOutOfRangeException(nameof(levelName),
													  levelName,
													  "Level with this name does not exist");
			}

			public void SimulatePressingBackButton()
			{
				MenuUIManager.SwitchBack();
			}
#endif
		}

		//TODO: Check this if it is null, prohibit changing when the screen is visible etc.
		public GamePack Package { get; set; }

		protected override ScreenBase ScreenInstance {
			get => screen;
			set => screen = (Screen)value;
		}

		Screen screen;

		public LevelPickingScreen(MenuUIManager menuUIManager)
			:base(menuUIManager)
		{ }

		public override void ExecuteAction(MenuScreenAction action)
		{
			if (action is LevelPickScreenAction myAction)
			{
				switch (myAction.Action)
				{
					case LevelPickScreenAction.Actions.EditNew:
						screen.SimulateEditPickingNewLevel();
						break;
					case LevelPickScreenAction.Actions.Edit:
						screen.SimulateEditPickingLevel(myAction.LevelName);
						break;
					case LevelPickScreenAction.Actions.Play:
						screen.SimulatePlayPickingLevel(myAction.LevelName);
						break;
					case LevelPickScreenAction.Actions.Back:
						screen.SimulatePressingBackButton();
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			else
			{
				throw new ArgumentException("Action does not belong to the current screen", nameof(action));
			}
		}

		public override void Show()
		{
			if (ScreenInstance != null) {
				return;
			}

			screen = new Screen(this);
		}
	
	}
}
