using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho;
using MHUrho.Logic;
using MHUrho.UserInterface;
using Urho.Gui;
using Urho.Resources;

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

			using (var packageUILayout = gameUI.Game.PackageManager.GetXmlFile("Assets/UI/BaseLayout.xml", true)) {
				var style = gameUI.Game.PackageManager.GetXmlFile("Assets/UI/UIStyle.xml", true);
				PackageRoot = ui.UI.LoadLayout(packageUILayout, style);
				PackageRoot.SetDefaultStyle(style);
				gameUI.GameUIRoot.AddChild(PackageRoot);
			}



			if (gameUI.Game.Config.DebugHUD) {
				using (Window topBar = (Window) PackageRoot.GetChild("TopBar")) {
					topBar.MinWidth = topBar.MinWidth - 100;
					topBar.Width = topBar.Width - 100;
					topBar.MaxWidth = topBar.MinWidth;
				}
					
			}

			resourceDisplays = new Dictionary<ResourceType, Text>();
			var resourceDisplay = PackageRoot.GetChild("ResourceDisplay", true);
			foreach (var resourceType in level.Package.ResourceTypes) {
				var display = (Text) resourceDisplay.GetChild(resourceType.Name);
				resourceDisplays.Add(resourceType, display);
			}

			resourceDisplay.Visible = showResources;
		}

		public void UpdateResourceDisplay(IReadOnlyDictionary<ResourceType, double> values)
		{
			foreach (var display in resourceDisplays) {
				if (values.TryGetValue(display.Key, out double value)) {
					display.Value.Value = display.Key.Name + ": " + value.ToString("F2");
				}
				else {
					display.Value.Value = display.Key.Name + ": 0";
				}
			}
		}

		public void LoadLayoutToUI(string path, string stylePath)
		{
			using (var layout = gameUI.Game.PackageManager.GetXmlFile(path, true)) {
				using (var styleFile = gameUI.Game.PackageManager.GetXmlFile(stylePath)) {
					PackageRoot.AddChild(gameUI.UI.LoadLayout(layout, styleFile));
				}
			}
			
		}

		public XmlFile GetStyleFile()
		{
			return gameUI.Game.PackageManager.GetXmlFile("Assets/UI/UIStyle.xml");
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
