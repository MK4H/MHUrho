using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.UserInterface
{
	
	class LevelPickingLevelItem : ExpandingListItem {

		public LevelRep Level { get; private set; }

		public LevelPickingLevelItem(LevelRep level, MHUrhoApp game)
			:base(game, "UI/LevelItemStyle.xml", true)
		{
			this.Level = level;


			XmlFile styleFile = game.PackageManager.GetXmlFile("UI/LevelItemStyle.xml", true);

			UIElement fixedElementContents = game.UI.LoadLayout(game.PackageManager.GetXmlFile("UI/LevelItemFixedLayout.xml", true),
																styleFile);
			FixedElement.AddChild(fixedElementContents);


			UIElement expandingElementContents = game.UI.LoadLayout(game.PackageManager.GetXmlFile("UI/LevelItemExpandingLayout.xml", true),
																	 styleFile);

			ExpandingElement.AddChild(expandingElementContents);

			var thumbnailElement = (BorderImage)fixedElementContents.GetChild("Thumbnail", true);
			thumbnailElement.Texture = level.Thumbnail;
			thumbnailElement.ImageRect = new IntRect(0, 0, level.Thumbnail.Width, level.Thumbnail.Height);

			var nameElement = (Text)CheckBox.GetChild("NameText", true);
			nameElement.Value = level.Name;

			((Text)ExpandingElement.GetChild("DescriptionText", true)).Value = level.Description;
		}
	}

	class LevelPickingNewLevelItem : ExpandingListItem
	{
		public LevelPickingNewLevelItem(MHUrhoApp game)
			:base(game, "UI/LevelItemStyle.xml", false)
		{
			UIElement fixedElementContets =
				game.UI.LoadLayout(game.PackageManager.GetXmlFile("UI/NewLevelItemLayout.xml", true),
									game.PackageManager.GetXmlFile("UI/LevelItemStyle.xml", true));

			FixedElement.AddChild(fixedElementContets);
		}
	}
}
