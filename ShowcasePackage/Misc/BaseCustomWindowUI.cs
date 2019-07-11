using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.UserInterface.MandK;
using ShowcasePackage.Buildings;
using Urho.Gui;

namespace ShowcasePackage.Misc
{
	class BaseCustomWindowUI : IDisposable {
		string name;
		string cost;

		public string Name {
			get => name;
			set {
				name = value;
				FillText();
			}
		}

		public string Cost {
			get => cost;
			set {
				cost = value;
				FillText();
			}
		}

		readonly UIElement uiElem;
		readonly Text nameText;
		readonly Text costText;

		public BaseCustomWindowUI(GameUI ui, string name, string cost)
		{
			this.name = name;
			this.cost = cost;

			if ((uiElem = ui.CustomWindow.GetChild("BaseBuildingCWUI")) == null)
			{
				ui.CustomWindow.LoadLayout("Assets/UI/BaseBuildingCWUI.xml");
				uiElem = ui.CustomWindow.GetChild("BaseBuildingCWUI");
			}

			nameText = (Text)uiElem.GetChild("Name");
			costText = (Text)uiElem.GetChild("Cost");

			uiElem.Visible = false;
		}

		public void Show()
		{
			uiElem.Visible = true;
			FillText();
		}

		public void Hide()
		{
			uiElem.Visible = false;
			ClearText();
		}

		public void Dispose()
		{
			uiElem.Dispose();
			nameText.Dispose();
			costText.Dispose();
		}

		void FillText()
		{
			if (uiElem.Visible) {
				nameText.Value = Name;
				costText.Value = Cost;
			}
		}

		void ClearText()
		{
			nameText.Value = "";
			costText.Value = "";
		}
	}
}
