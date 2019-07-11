using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using MHUrho.Helpers;
using MHUrho.UserInterface;
using Urho;

namespace MHUrho.StartupManagement
{

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

		public static StartupOptions FromCommandLineParams(string[] args, FileManager files)
		{
			ActionManager uiActions = null;

			IEnumerator<string> argsEnumerator = (from arg in args select arg).GetEnumerator();

			while (argsEnumerator.MoveNext()) {
				switch (argsEnumerator.Current) {
					case "-ui":
						uiActions = GetUIActions(argsEnumerator, files);
						break;
				}
			}

			argsEnumerator.Dispose();

			return new StartupOptions(uiActions);
		}

		static ActionManager GetUIActions(IEnumerator<string> argsEnumerator, FileManager files)
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
							? files.OpenDynamicFile(argsEnumerator.Current,
															System.IO.FileMode.Open,
															System.IO.FileAccess.Read)
							: files.OpenStaticFileRO(argsEnumerator.Current);
				XDocument xmlFile = XDocument.Load(file);

				try
				{
					return new ActionManager(xmlFile, files);
				}
				catch (IOException e)
				{
					//NOTE: This may mean we could not open xml schema
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

		class Execution {
			readonly MHUrhoApp game;
			readonly IEnumerator<MenuScreenAction> actions;

			public Execution(MHUrhoApp game, IEnumerator<MenuScreenAction> actions)
			{
				this.game = game;
				this.actions = actions;
			}

			public void TriggerNext()
			{
				if (actions.MoveNext()) {
					game.MenuController.ExecuteActionOnCurrentScreen(actions.Current);
				}
				else {
					game.MenuController.ScreenChanged -= TriggerNext;
				}
			}
		}

		static readonly string SchemaPath = Path.Combine("Data", "Schemas", "MenuActions.xsd");

		readonly List<MenuScreenAction> actions;

		/// <summary>
		/// Creates new action manager from the given <paramref name="xmlFile"/>.
		/// </summary>
		/// <param name="xmlFIle">XML file to parse the actions from.</param>
		/// <param name="files">File management system.</param>
		/// <exception cref="IOException">Occurs when the <paramref name="xmlFilePath"/> is not valid path or the file could not be opened</exception>
		/// <exception cref="XmlSchemaValidationException">Occurs when <paramref name="xmlFilePath"/> does not conform to the schema at Schemas/MenuActions.xsd</exception>
		public ActionManager(XDocument xmlFile, FileManager files)
		{
			var schema = new XmlSchemaSet();
			schema.Add(MenuScreenAction.XMLNamespace.NamespaceName,
						XmlReader.Create(files.OpenStaticFileRO(SchemaPath)));

			xmlFile.Validate(schema, null);

			actions = MenuScreenAction.Parse(xmlFile);
		}

		public void RunActions(MHUrhoApp game)
		{
			var exec = new Execution(game, actions.GetEnumerator());
			game.MenuController.ScreenChanged += exec.TriggerNext;
			exec.TriggerNext();
		}
	}

	public abstract class MenuScreenAction {
		public static XNamespace XMLNamespace = "http://www.MobileHold.cz/MenuActions.xsd";

		static readonly Dictionary<string, Func<XElement, MenuScreenAction>> ScreenActions
			= new Dictionary<string, Func<XElement, MenuScreenAction>>
			{
				{MainMenuAction.Name, MainMenuAction.FromXml},
				{PackagePickScreenAction.Name, PackagePickScreenAction.FromXml },
				{LevelPickScreenAction.Name, LevelPickScreenAction.FromXml },
				{LevelCreationScreenAction.Name, LevelCreationScreenAction.FromXml },
				{LevelSettingsScreenAction.Name, LevelSettingsScreenAction.FromXml },
				{LoadingScreenAction.Name, LoadingScreenAction.FromXml }
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
					select ScreenActions[actionName](element)).ToList();
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

		protected MainMenuAction(Actions action)
		{
			this.Action = action;
		}

		public static MainMenuAction GetStartAction()
		{
			return new MainMenuAction(Actions.Start);
		}

		public static MainMenuAction GetLoadAction()
		{
			return new MainMenuAction(Actions.Load);
		}

		public static MainMenuAction GetOptionsAction()
		{
			return new MainMenuAction(Actions.Options);
		}

		public static MainMenuAction GetAboutAction()
		{
			return new MainMenuAction(Actions.About);
		}

		public static MainMenuAction GetExitAction()
		{
			return new MainMenuAction(Actions.Exit);
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

		protected PackagePickScreenAction(Actions action, string packageName = null)
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

		public static PackagePickScreenAction GetPickAction(string packageName)
		{
			if (packageName == null) {
				throw new ArgumentNullException(nameof(packageName), "Package name cannot be null");
			}

			return new PackagePickScreenAction(Actions.Pick, packageName);
		}

		public static PackagePickScreenAction GetBackAction()
		{
			return new PackagePickScreenAction(Actions.Back);
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

		public string LevelName {
			get {
				if (Action != Actions.Edit && Action != Actions.Play) {
					throw new InvalidOperationException("LevelName is only valid with Actions Edit and Play");
				}

				return levelName;
			}
		}

		readonly string levelName;

		protected LevelPickScreenAction(Actions action, string levelName = null)
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

		public static LevelPickScreenAction GetEditNewAction()
		{
			return new LevelPickScreenAction(Actions.EditNew);
		}

		public static LevelPickScreenAction GetEditAction(string levelName)
		{
			return new LevelPickScreenAction(Actions.Edit, levelName);
		}

		public static LevelPickScreenAction GetPlayAction(string levelName)
		{
			return new LevelPickScreenAction(Actions.Play, levelName);
		}

		public static LevelPickScreenAction GetBackAction()
		{
			return new LevelPickScreenAction(Actions.Back);
		}

		public static LevelPickScreenAction FromXml(XElement element)
		{
			CheckName(Name, element);

			//Element should exist and its value should be correct thanks to schema validation
			string actionStr = element.Element(XMLNamespace + "action").Value;

			Actions action = StringToAction(actionStr);

			if (action == Actions.Edit || action == Actions.Play)
			{
				XElement valuesElement = GetValuesElement(element);

				return BuildActionWithLevelName(action, valuesElement);
			}
			else
			{
				return new LevelPickScreenAction(action);
			}
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

		public string LogicTypeName {
			get {
				if (Action != Actions.Edit)
				{
					throw new InvalidOperationException("LogicTypeName is only valid with action Actions.Edit");
				}

				return logicTypeName;
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

		readonly string levelName;
		readonly string description;
		readonly string thumbnailPath;
		readonly string logicTypeName;
		readonly IntVector2 mapSize;

		protected LevelCreationScreenAction(Actions action,
										string levelName = null,
										string description = null,
										string thumbnailPath = null,
										string logicTypeName = null,
										IntVector2? mapSize = null)
		{
			if (action == Actions.Back &&
				(levelName != null ||
				description != null ||
				thumbnailPath != null ||
				logicTypeName != null ||
				mapSize != null)) {

				throw new ArgumentException("Arguments cannot have a value with action Back");
			}
			else if (action == Actions.Edit &&
					(levelName == null ||
					description == null ||
					thumbnailPath == null ||
					logicTypeName == null ||
					mapSize == null)) {

				throw new ArgumentNullException("levelName, description, thumbnailPath, pluginPath or mapSize",
												"Arguments cannot be null with action Edit");
			}

			this.Action = action;

			this.levelName = levelName;
			this.description = description;
			this.thumbnailPath = thumbnailPath;
			this.logicTypeName = logicTypeName;
			this.mapSize = mapSize ?? new IntVector2();
		}


		public static LevelCreationScreenAction GetEditAction(string levelName,
																string description,
																string thumbnailPath,
																string logicTypeName,
																IntVector2 mapSize)
		{
			return new LevelCreationScreenAction(Actions.Edit,
												levelName,
												description,
												thumbnailPath,
												logicTypeName,
												mapSize);
		}

		public static LevelCreationScreenAction GetBackAction()
		{
			return new LevelCreationScreenAction(Actions.Back);
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
			string typeName = values.Element(XMLNamespace + "logicTypeName").Value;
			IntVector2 mapSize = XmlHelpers.GetIntVector2(values.Element(XMLNamespace + "mapSize"));
			return new LevelCreationScreenAction(action, levelName, description, thumbnailPath, typeName, mapSize);
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
		public enum Actions { Play, Back };

		public static string Name = "levelSettings";

		public Actions Action { get; private set; }

		public string NeutralPlayerTypeName {
			get {
				if (Action != Actions.Play)
				{
					throw new InvalidOperationException("NeutralPlayerTypeName is only valid with action Actions.Play");
				}

				return neutralPlayerLogicName;
			}
		}

		public Tuple<string, int> HumanPlayer {
			get {
				if (Action != Actions.Play)
				{
					throw new InvalidOperationException("HumanPlayer is only valid with action Actions.Play");
				}

				return humanPlayer;
			}
		}

		public IReadOnlyList<Tuple<string, int>> AIPlayers {
			get {
				if (Action != Actions.Play)
				{
					throw new InvalidOperationException("AIPlayers is only valid with action Actions.Play");
				}

				return aiPlayers;
			}
		}

		readonly string neutralPlayerLogicName;
		readonly Tuple<string, int> humanPlayer;
		readonly List<Tuple<string, int>> aiPlayers;

		protected LevelSettingsScreenAction(Actions action,
											string neutralPlayerLogicName = null,
											Tuple<string,int> humanPlayer = null,
											List<Tuple<string, int>> aiPlayers = null)
		{
			if (action == Actions.Back &&
				(neutralPlayerLogicName != null ||
				humanPlayer != null ||
				aiPlayers != null)) {
				throw new ArgumentException("Argument value was incorrectly provided for action back");
			}
			else if (action == Actions.Play &&
					(neutralPlayerLogicName == null ||
					humanPlayer == null ||
					aiPlayers == null)) {
				throw new ArgumentException("Argument was null for action play");
			}

			this.neutralPlayerLogicName = neutralPlayerLogicName;
			this.humanPlayer = humanPlayer;
			this.aiPlayers = aiPlayers;
		}

		public static LevelSettingsScreenAction FromXml(XElement element)
		{
			CheckName(Name, element);

			//Element should exist and its value should be correct thanks to schema validation
			string actionStr = element.Element(XMLNamespace + "action").Value;

			Actions action = StringToAction(actionStr);

			if (action == Actions.Play)
			{
				XElement valuesElement = GetValuesElement(element);

				return BuildActionWithValues(action, valuesElement);
			}
			else
			{
				return new LevelSettingsScreenAction(Actions.Back);
			}
		}

		/// <summary>
		/// Creates an action for <see cref="UserInterface.LevelSettingsScreen"/> that executes,
		/// replacing user input
		/// </summary>
		/// <param name="neutralPlayerLogicName">Logic name of the neutral player</param>
		/// <param name="humanPlayer">Logic name and TeamID of the human player</param>
		/// <param name="aiPlayers">Logic names and TeamIDs of the AI players</param>
		/// <returns></returns>
		public static LevelSettingsScreenAction GetPlayAction(string neutralPlayerLogicName,
															Tuple<string,int> humanPlayer,
															List<Tuple<string, int>> aiPlayers)
		{
			return new LevelSettingsScreenAction(Actions.Play, neutralPlayerLogicName, humanPlayer, aiPlayers);
		}

		public static LevelSettingsScreenAction GetBackAction()
		{
			return new LevelSettingsScreenAction(Actions.Back);
		}

		static LevelSettingsScreenAction BuildActionWithValues(Actions action, XElement values)
		{
			const string logicNameAttribute = "typeName";
			const string teamIDAttribute = "teamID";
			const string neutralPlayerElementName = "neutralPlayer";
			const string humanPlayerElementName = "humanPlayer";
			const string aiPlayerElementName = "aiPlayer";

			//if valuesElement is not null, schema should guarantee that there are the value elements
			XElement neutralPlayerElement = values.Element(XMLNamespace + neutralPlayerElementName);
			string neutralPlayerLogicName = neutralPlayerElement.Attribute(logicNameAttribute).Value;
			XElement humanPlayerElement = values.Element(XMLNamespace + humanPlayerElementName);
			string humanPlayerLogicName = humanPlayerElement.Attribute(logicNameAttribute).Value;
			int humanPlayerTeamID = int.Parse(humanPlayerElement.Attribute(teamIDAttribute).Value);
			//TODO: Maybe correct the paths

			List<Tuple<string, int>> aiPlayers = new List<Tuple<string, int>>();

			aiPlayers.AddRange(from element in values.Elements(XMLNamespace + aiPlayerElementName)
								select Tuple.Create(element.Attribute(logicNameAttribute).Value,
													int.Parse(element.Attribute(teamIDAttribute).Value)));

			return new LevelSettingsScreenAction(action, 
												neutralPlayerLogicName, 
												Tuple.Create(humanPlayerLogicName, humanPlayerTeamID), 
												aiPlayers);
		}

		static Actions StringToAction(string stringRepr)
		{
			// STRINGS HAVE TO MATCH THOSE IN THE SCHEMA
			switch (stringRepr)
			{
				case "back":
					return Actions.Back;
				case "play":
					return Actions.Play;
				default:
					throw new ArgumentOutOfRangeException(nameof(stringRepr), stringRepr, "Unknown action string");
			}
		}
	}

	public class LoadingScreenAction : MenuScreenAction {
		public enum Actions { None };

		public static string Name = "loading";

		public Actions Action { get; private set; }

		protected LoadingScreenAction(Actions action)
		{
			this.Action = action;
		}

		public static LoadingScreenAction GetNoneAction()
		{
			return new LoadingScreenAction(Actions.None);
		}

		public static LoadingScreenAction FromXml(XElement element)
		{
			CheckName(Name, element);

			//Element should exist and its value should be correct thanks to schema validation
			string actionStr = element.Element(XMLNamespace + "action").Value;

			return new LoadingScreenAction(StringToAction(actionStr));
		}

		static Actions StringToAction(string stringRepr)
		{
			// STRINGS HAVE TO MATCH THOSE IN THE SCHEMA
			switch (stringRepr)
			{
				case "none":
					return Actions.None;
				default:
					throw new ArgumentOutOfRangeException(nameof(stringRepr), stringRepr, "Unknown action string");
			}
		}
	}
}
