using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.Logic;
using MHUrho.UserInterface;
using Urho.Gui;

namespace ShowcasePackage.Misc
{
	public class PackageUI : IDisposable
	{
		public UIElement PackageRoot { get; private set; }

		readonly GameUIManager gameUI;

		readonly Dictionary<ResourceType, Text> resourceDisplays;

		public PackageUI(GameUIManager ui, ILevelManager level, bool showResources)
		{
			this.gameUI = ui;

			var packageUILayout = gameUI.Game.PackageManager.GetXmlFile("Assets/UI/BaseLayout.xml", true);
			PackageRoot = ui.UI.LoadLayout(packageUILayout);
			gameUI.GameUIRoot.AddChild(PackageRoot);

			var defaultStyle = gameUI.Game.PackageManager.GetXmlFile("Assets/UI/UIStyle.xml", true); ;
			PackageRoot.SetDefaultStyle(defaultStyle);

			resourceDisplays = new Dictionary<ResourceType, Text>();
			var resourceDisplay = PackageRoot.GetChild("ResourceDisplay", true);
			foreach (var resourceType in level.Package.ResourceTypes) {
				var display = resourceDisplay.CreateText();
				resourceDisplays.Add(resourceType, display);
			}

			resourceDisplay.Visible = showResources;
		}

		public void UpdateResourceDisplay(IReadOnlyDictionary<ResourceType, double> values)
		{
			foreach (var display in resourceDisplays) {
				if (values.TryGetValue(display.Key, out double value)) {
					display.Value.Value = display.Key.Name + ": " + value;
				}
				else {
					display.Value.Value = display.Key.Name + ": 0";
				}
			}
		}

		public void LoadLayoutToUI(string path)
		{
			gameUI.UI.LoadLayoutToElement(PackageRoot, gameUI. Game.ResourceCache, path);
		}

		public void Dispose()
		{
			foreach (var display in resourceDisplays) {
				display.Value.Dispose();
			}

			PackageRoot.Remove();
			PackageRoot.Dispose();
		}
	}
}
