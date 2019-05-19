using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class PackageListItem : ExpandingListItem
    {
		public GamePackRep Pack { get; private set; }

		readonly Text descriptionText;

		public PackageListItem(GamePackRep pack, MHUrhoApp game)
			:base(game, "UI/AvailablePackItemStyle.xml", true)
		{
			this.Pack = pack;
			UIElement fixedElementContents = game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/AvailablePackItemFixedLayout.xml", true),
																PackageManager.Instance.GetXmlFile("UI/AvailablePackItemStyle.xml", true));

			FixedElement.AddChild(fixedElementContents);

			var thumbnail = (BorderImage)FixedElement.GetChild("Thumbnail", true);
			thumbnail.Texture = pack.Thumbnail;
			thumbnail.ImageRect = new IntRect(0, 0, pack.Thumbnail.Width, pack.Thumbnail.Height);

			var name = (Text)FixedElement.GetChild("NameText", true);
			name.Value = pack.Name;


			UIElement expandingElementContents =
				game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/AvailablePackItemExpandingLayout.xml", true),
									PackageManager.Instance.GetXmlFile("UI/AvailablePackItemStyle.xml", true));

			ExpandingElement.AddChild(expandingElementContents);

			descriptionText = (Text)ExpandingElement.GetChild("DescriptionText", true);
			descriptionText.Value = pack.Description;
		}

		public override void Dispose()
		{
			descriptionText.Dispose();
			base.Dispose();
		}
    }
}
