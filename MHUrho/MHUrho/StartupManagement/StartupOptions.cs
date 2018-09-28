using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using MHUrho.Helpers;
using Urho;

namespace MHUrho.StartupManagement
{
#if DEBUG
	/// <summary>
	/// Simple command line parsing for debugging
	/// </summary>
	public class StartupOptions
	{

		public ActionManager UIActions { get; private set; }

		StartupOptions(ActionManager uiActions)
		{
			UIActions = uiActions;
		}

		public static StartupOptions FromCommandLineParams(string[] args)
		{
			ActionManager uiActions = null;

			IEnumerator<string> argsEnumerator = (from arg in args select arg).GetEnumerator();

			while (argsEnumerator.MoveNext()) {
				switch (argsEnumerator.Current) {
					case "-ui":
						uiActions = GetUIActions(argsEnumerator);
						break;
				}
			}

			argsEnumerator.Dispose();

			return new StartupOptions(uiActions);
		}

		static ActionManager GetUIActions(IEnumerator<string> argsEnumerator)
		{
			bool fromDynamic = true;
			if (argsEnumerator.MoveNext()) {
				//Whether to open from dynamic directory or static directory, default is static
				
				if (argsEnumerator.Current.Length == 1) {
					switch (argsEnumerator.Current) {
						case "s":
							fromDynamic = false;
							if (!argsEnumerator.MoveNext())
							{
								throw new ArgumentException("-ui missing a xmlFilePath argument, use -ui [s|d] xmlFilePath");
							}
							break;
						case "d":
							fromDynamic = true;
							if (!argsEnumerator.MoveNext())
							{
								throw new ArgumentException("-ui missing a xmlFilePath argument, use -ui [s|d] xmlFilePath");
							}
							break;
						default:
							//One character path is also possible
							break;
					}
				}
			}
			else {
				throw new ArgumentException("-ui missing a xmlFilePath argument, use -ui [s|d] xmlFilePath");
			}
			

			Stream file = null;
			try {
				file = fromDynamic
							? MyGame.Files.OpenDynamicFile(argsEnumerator.Current,
															System.IO.FileMode.Open,
															System.IO.FileAccess.Read)
							: MyGame.Files.OpenStaticFileRO(argsEnumerator.Current);
				XDocument xmlFile = XDocument.Load(file);

				try
				{
					return new ActionManager(xmlFile);
				}
				catch (IOException e)
				{
					//TODO: This means could not open xml schema
					throw new ArgumentException("-ui xmlFilePath, could not read actions from the file xmlFilePath", e);
				}
				catch (XmlSchemaValidationException e)
				{
					throw new
						ArgumentException("-ui xmlFilePath, xml did not conform to the Schemas/MenuActions.xsd schema", e);
				}
			}
			finally {
				file?.Dispose();
			}

			
		}
	}

	public class ActionManager {

		static readonly string SchemaPath = Path.Combine("Data", "Schemas", "MenuActions.xsd");

		readonly List<MenuScreenAction> actions;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xmlFilePath"></param>
		/// <exception cref="IOException">Occurs when the <paramref name="xmlFilePath"/> is not valid path or the file could not be opened</exception>
		/// <exception cref="XmlSchemaValidationException">Occurs when <paramref name="xmlFilePath"/> does not conform to the schema at Schemas/MenuActions.xsd</exception>
		public ActionManager(XDocument xmlFile)
		{
			try {


				var schema = new XmlSchemaSet();
				schema.Add(MenuScreenAction.XMLNamespace.NamespaceName,
							XmlReader.Create(MyGame.Files.OpenStaticFileRO(SchemaPath)));

				xmlFile.Validate(schema, null);

				actions = MenuScreenAction.Parse(xmlFile);
			}
			catch (XmlSchemaValidationException e) {
				//TODO: maybe log
				throw;
			}
			
		}

		public void RunActions(MyGame game)
		{
			foreach (var action in actions) {
				game.MenuController.ExecuteActionOnCurrentScreen(action);
			}
		}
	}

	public abstract class MenuScreenAction {
		public static XNamespace XMLNamespace = "http://www.MobileHold.cz/MenuActions.xsd";

		static Dictionary<string, Func<XElement, MenuScreenAction>> screenActions
			= new Dictionary<string, Func<XElement, MenuScreenAction>>
			{
				{MainMenuAction.Name, MainMenuAction.FromXml},
				{PackagePickScreenAction.Name, PackagePickScreenAction.FromXml },
				{LevelPickScreenAction.Name, LevelPickScreenAction.FromXml },
				{LevelCreationScreenAction.Name, LevelCreationScreenAction.FromXml },
				{LevelSettingsScreenAction.Name, LevelSettingsScreenAction.FromXml }
			};
																					

		/// <summary>
		/// Parses XML file into MenuScreenActions, that should go through UI screens without user input
		/// </summary>
		/// <param name="xml">Validated xml document representing the actions</param>
		/// <returns>The list of actions that should be executed in that order</returns>
		public static List<MenuScreenAction> Parse(XDocument xml)
		{
			XElement root = xml.Root;

			return (from element in root.Elements()
					let actionName = element.Name.LocalName
					select screenActions[actionName](element)).ToList();
		}

		protected static XElement GetValuesElement(XElement screenActionElement)
		{
			XElement values = screenActionElement.Element(XMLNamespace + "values");

			if (values == null)
			{
				throw new ArgumentException("Missing values element", nameof(screenActionElement));
			}

			return values;
		}

		protected static void CheckName(string wantedName, XElement screenActionElement)
		{
			string name = screenActionElement.Name.LocalName;
			if (name != wantedName)
			{
				throw new ArgumentException("Invalid xml element for this type", nameof(wantedName));
			}
		}
	}

	public class MainMenuAction : MenuScreenAction {

		public enum Actions { Start, Load, Options, About, Exit}

		public static string Name = "mainMenu";

		public Actions Action { get; private set; }

		public MainMenuAction(Actions action)
		{
			this.Action = action;
		}

		public static MainMenuAction FromXml(XElement element)
		{
			CheckName(Name, element);

			//Element should exist and its value should be correct thanks to schema validation
			string actionStr = element.Element(XMLNamespace + "action").Value;

			return new MainMenuAction(StringToAction(actionStr));
		}

		static Actions StringToAction(string stringRepr)
		{
			// STRINGS HAVE TO MATCH THOSE IN THE SCHEMA
			switch (stringRepr) {
				case "start":
					return Actions.Start;
				case "load":
					return Actions.Load;
				case "options":
					return Actions.Options;
				case "about":
					return Actions.About;
				case "exit":
					return Actions.Exit;
				default:
					throw new ArgumentOutOfRangeException(nameof(stringRepr), stringRepr, "Unknown action string");
			}
		}
	}

	public class PackagePickScreenAction : MenuScreenAction
	{
		public enum Actions { Pick, Back };

		public static string Name = "packagePicking";

		public Actions Action { get; private set; }

		public string PackageName {
			get {
				if (Action != Actions.Pick) {
					throw new InvalidOperationException("PackageName is only valid with action Pick");
				}

				return packageName;
			}
		}

		readonly string packageName;

		public PackagePickScreenAction(Actions action, string packageName = null)
		{
			this.Action = action;

			if ((action == Actions.Pick) && packageName == null)
			{
				throw new ArgumentNullException(nameof(packageName),
												"Package name cannot be null with action Pick");
			}
			else if ((action == Actions.Back) && packageName != null) {
				throw new ArgumentException("Package name cannot have value with action Back", 
											nameof(packageName));
			}

			this.packageName = packageName;
		}

		public static PackagePickScreenAction FromXml(XElement element)
		{
			CheckName(Name, element);

			//Element should exist and its value should be correct thanks to schema validation
			string actionStr = element.Element(XMLNamespace + "action").Value;

			Actions action = StringToAction(actionStr);

			if (action == Actions.Pick)
			{
				XElement valuesElement = GetValuesElement(element);
				return BuildPickAction(valuesElement);
			}
			else
			{
				return new PackagePickScreenAction(Actions.Back);
			}
		}

		static Actions StringToAction(string stringRepr)
		{
			// STRINGS HAVE TO MATCH THOSE IN THE SCHEMA
			switch (stringRepr)
			{
				case "pick":
					return Actions.Pick;
				case "back":
					return Actions.Back;
				default:
					throw new ArgumentOutOfRangeException(nameof(stringRepr), stringRepr, "Unknown action string");
			}
		}

		static PackagePickScreenAction BuildPickAction(XElement valuesElement)
		{
			//if valuesElement is not null, schema should guarantee that there is packageName element
			string packageName = valuesElement.Element(XMLNamespace + "packageName").Value;
			return new PackagePickScreenAction(Actions.Pick, packageName);
		}

	}

	public class LevelPickScreenAction : MenuScreenAction
	{
		public enum Actions { EditNew, Edit, Play, Back }

		public static string Name = "levelPicking";

		public Actions Action { get; private set; }

		public static LevelPickScreenAction FromXml(XElement element)
		{
			CheckName(Name, element);

			//Element should exist and its value should be correct thanks to schema validation
			string actionStr = element.Element(XMLNamespace + "action").Value;

			Actions action = StringToAction(actionStr);

			if (action == Actions.Edit || action == Actions.Play) {
				XElement valuesElement = GetValuesElement(element);

				return BuildActionWithLevelName(action, valuesElement);
			}
			else {
				return new LevelPickScreenAction(action);
			}
		}

		public string LevelName {
			get {
				if (Action != Actions.Edit && Action != Actions.Play) {
					throw new InvalidOperationException("LevelName is only valid with Actions Edit and Play");
				}

				return levelName;
			}
		}

		readonly string levelName;

		public LevelPickScreenAction(Actions action, string levelName = null)
		{
			this.Action = action;

			if ((action == Actions.Edit || action == Actions.Play) && levelName == null) {
				throw new ArgumentNullException(nameof(levelName),
												"Level name cannot be null with actions Edit and Play");
			}
			else if ((action == Actions.Back || action == Actions.EditNew) && levelName != null) {
				throw new ArgumentException("Level name cannot have value with actions back and editNew",
											nameof(levelName));
			}

			this.levelName = levelName;
		}

		static LevelPickScreenAction BuildActionWithLevelName(Actions action, XElement values)
		{
			//if valuesElement is not null, schema should guarantee that there is levelName element
			string levelName = values.Element(XMLNamespace + "levelName").Value;
			return new LevelPickScreenAction(action, levelName);
		}

		static Actions StringToAction(string stringRepr)
		{
			// STRINGS HAVE TO MATCH THOSE IN THE SCHEMA
			switch (stringRepr)
			{
				case "editnew":
					return Actions.EditNew;
				case "edit":
					return Actions.Edit;
				case "play":
					return Actions.Play;
				case "back":
					return Actions.Back;
				default:
					throw new ArgumentOutOfRangeException(nameof(stringRepr), stringRepr, "Unknown action string");
			}
		}
	}

	public class LevelCreationScreenAction : MenuScreenAction
	{
		public enum Actions { Edit, Back}

		public static string Name = "levelCreation";

		public Actions Action { get; private set; }

	
		public string LevelName {
			get {
				if (Action != Actions.Edit) {
					throw new InvalidOperationException("NewLevelName is only valid with action Actions.Edit");
				}

				return levelName;
			}
		}

		public string Description {
			get {
				if (Action != Actions.Edit)
				{
					throw new InvalidOperationException("Description is only valid with action Actions.Edit");
				}

				return description;
			}
		}

		public string ThumbnailPath {
			get {
				if (Action != Actions.Edit)
				{
					throw new InvalidOperationException("ThumbnailPath is only valid with action Actions.Edit");
				}

				return thumbnailPath;
			}
		}

		public string PluginPath {
			get {
				if (Action != Actions.Edit)
				{
					throw new InvalidOperationException("PluginPath is only valid with action Actions.Edit");
				}

				return pluginPath;
			}
		}

		public IntVector2 MapSize {
			get {
				if (Action != Actions.Edit)
				{
					throw new InvalidOperationException("MapSize is only valid with action Actions.Edit");
				}

				return mapSize;
			}
		}

		string levelName;
		string description;
		string thumbnailPath;
		string pluginPath;
		IntVector2 mapSize;

		public LevelCreationScreenAction(Actions action,
										string levelName = null,
										string description = null,
										string thumbnailPath = null,
										string pluginPath = null,
										IntVector2? mapSize = null)
		{
			if (action == Actions.Back &&
				(levelName != null ||
				description != null ||
				thumbnailPath != null ||
				pluginPath != null ||
				mapSize != null)) {

				throw new ArgumentException("Arguments cannot have a value with action Back");
			}
			else if (action == Actions.Edit &&
					(levelName == null ||
					description == null ||
					thumbnailPath == null ||
					pluginPath == null ||
					mapSize == null)) {

				throw new ArgumentNullException("levelName, description, thumbnailPath, pluginPath or mapSize",
												"Arguments cannot be null with action Edit");
			}

			this.Action = action;

			this.levelName = levelName;
			this.description = description;
			this.thumbnailPath = thumbnailPath;
			this.pluginPath = pluginPath;
			this.mapSize = mapSize ?? new IntVector2();
		}


		public static LevelCreationScreenAction FromXml(XElement element)
		{
			CheckName(Name, element);

			//Element should exist and its value should be correct thanks to schema validation
			string actionStr = element.Element(XMLNamespace + "action").Value;

			Actions action = StringToAction(actionStr);

			if (action == Actions.Edit)
			{
				XElement valuesElement = GetValuesElement(element);

				return BuildActionWithValues(action, valuesElement);
			}
			else
			{
				return new LevelCreationScreenAction(Actions.Back);
			}
		}

		static LevelCreationScreenAction BuildActionWithValues(Actions action, XElement values)
		{
			//if valuesElement is not null, schema should guarantee that there are the value elements
			string levelName = values.Element(XMLNamespace + "levelName").Value;
			string description = values.Element(XMLNamespace + "description").Value;
			//TODO: Maybe correct the paths
			string thumbnailPath = values.Element(XMLNamespace + "thumbnailPath").Value;
			string pluginPath = values.Element(XMLNamespace + "pluginPath").Value;
			IntVector2 mapSize = XmlHelpers.GetIntVector2(values.Element(XMLNamespace + "mapSize"));
			return new LevelCreationScreenAction(action, levelName, description, thumbnailPath, pluginPath, mapSize);
		}

		static Actions StringToAction(string stringRepr)
		{
			// STRINGS HAVE TO MATCH THOSE IN THE SCHEMA
			switch (stringRepr)
			{
				case "edit":
					return Actions.Edit;
				case "back":
					return Actions.Back;
				default:
					throw new ArgumentOutOfRangeException(nameof(stringRepr), stringRepr, "Unknown action string");
			}
		}
	}

	public class LevelSettingsScreenAction : MenuScreenAction {
		public enum Actions { Back };

		public static string Name = "levelSettings";

		public Actions Action { get; private set; }

		public static LevelSettingsScreenAction FromXml(XElement element)
		{
			throw new NotImplementedException();
		}

		static Actions StringToAction(string stringRepr)
		{
			// STRINGS HAVE TO MATCH THOSE IN THE SCHEMA
			switch (stringRepr)
			{
				case "back":
					return Actions.Back;
				default:
					throw new ArgumentOutOfRangeException(nameof(stringRepr), stringRepr, "Unknown action string");
			}
		}
	}
#endif
}
