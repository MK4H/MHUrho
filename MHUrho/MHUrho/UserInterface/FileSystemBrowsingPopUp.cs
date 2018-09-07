using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	interface IPathResult {
		string FullPath { get; }

		string RelativePath { get; }

		string BaseDir { get; }

		bool IsDirectory { get; }

		bool IsFile { get; }
	}

	[Flags]
	enum SelectOption {
		File = 1,
		Directory = 2
	}

	class FileSystemBrowsingPopUp 
    {

		class PathResult : IPathResult {
			public string FullPath => Path.Combine(BaseDir, RelativePath);

			public string RelativePath { get; private set; }

			public string BaseDir { get; private set; }
		

			public bool IsDirectory => (selectOption & SelectOption.Directory) != 0;
			public bool IsFile => (selectOption & SelectOption.File) != 0;

			readonly SelectOption selectOption;

			public PathResult(string baseDirPath, string relativeResultPath, SelectOption selectOption)
			{
				this.BaseDir = baseDirPath;
				this.RelativePath = relativeResultPath;
				this.selectOption = selectOption;
			}

			public override string ToString()
			{
				return $"[Path = {FullPath}]";
			}
		}

		class Screen : IDisposable {

			class NameTextPair : IComparable<NameTextPair>, IDisposable {
				public readonly string Name;
				public readonly Text Text;

				public readonly bool IsDirectory;

				public NameTextPair(string name, bool isDirectory)
				{
					Name = name;
					Text = new Text {
										Value = name,
										Visible = true
									};

					Text.SetStyle(isDirectory ? "DirectoryEntry" : "FileEntry", PackageManager.Instance.GetXmlFile("UI/FileBrowserStyle.xml"));

					IsDirectory = isDirectory;
				}

				public int CompareTo(NameTextPair other)
				{
					if (IsDirectory == other.IsDirectory) {
						return string.Compare(Name, other.Name, StringComparison.CurrentCultureIgnoreCase);
					}
					else {
						//Directory < !Directory, directories come first
						return other.IsDirectory.CompareTo(IsDirectory);
					}
				}

				public void Dispose()
				{
					Text.Dispose();
				}
			}

			MyGame Game => MyGame.Instance;

			readonly FileSystemBrowsingPopUp proxy;

			readonly TaskCompletionSource<IPathResult> taskCompletition;

			readonly Window window;
			readonly LineEdit pathEdit;
			readonly Button selectButton;
			readonly Button backButton;
			readonly ListView fileView;

			readonly string baseDir;

			readonly SelectOption selectOptions;

			List<NameTextPair> cDirEntries;

			string currentFileName;

			/// <summary>
			/// Path of the current directory relative to the baseDir
			/// </summary>
			string currentDirectory;
			
			bool currentDirMatch;
			NameTextPair totalMatch;

			string RelativePath => Path.Combine(currentDirectory, currentFileName);
			string AbsoluteCurrentDirectoryPath => Path.Combine(baseDir, currentDirectory);
			string AbsoluteCurrentFilePath => Path.Combine(baseDir, currentDirectory, currentFileName);

			/// <summary>
			/// Loads the screen layout and contents
			/// </summary>
			/// <param name="proxy"></param>
			/// <param name="baseDir"></param>
			/// <param name="startingRelativePath"></param>
			/// <param name="selectOptions"></param>
			/// <param name="taskCompletition"></param>
			/// <exception cref="ArgumentException">baseDir or startingRelativePath were wrong</exception>
			/// <exception cref="PathTooLongException"></exception>
			public Screen(FileSystemBrowsingPopUp proxy,
						string baseDir,
						string startingRelativePath,
						SelectOption selectOptions,
						TaskCompletionSource<IPathResult> taskCompletition)
			{
				this.proxy = proxy;
				this.baseDir = baseDir;
				this.selectOptions = selectOptions;
				this.taskCompletition = taskCompletition;
				this.cDirEntries = new List<NameTextPair>();

				Game.UI.LoadLayoutToElement(Game.UI.Root, Game.ResourceCache, "UI/FileBrowserLayout.xml");

				window = (Window)Game.UI.Root.GetChild("FileBrowserWindow");
				window.BringToFront();
				pathEdit = (LineEdit)window.GetChild("PathEdit", true);
				selectButton = (Button)window.GetChild("SelectButton", true);
				backButton = (Button)window.GetChild("BackButton", true);
				fileView = (ListView)window.GetChild("FileView", true);


				pathEdit.TextChanged += PathChanged;
				fileView.ItemSelected += OnItemSelected;
				fileView.ItemDoubleClicked += OnItemDoubleClicked;
				selectButton.Released += SelectButtonPressed;
				backButton.Released += BackButtonReleased;
				Game.Input.KeyDown += KeyDown;

				if (!string.IsNullOrEmpty(startingRelativePath)) {

					try {
						currentDirectory = Path.GetDirectoryName(startingRelativePath);
						currentFileName = Path.GetFileName(startingRelativePath);
					}
					catch (ArgumentException e) {
						Dispose();
						throw;
					}
					catch (PathTooLongException e) {
						Dispose();
						throw;
					}
				}
				else {
					currentDirectory = "";
					currentFileName = "";
				}

				if (!LoadDirectory(currentDirectory)) {
					Dispose();
					throw new ArgumentException("Invalid starting directory or baseDir path");
				}

				pathEdit.Text = Path.Combine(currentDirectory, currentFileName);

				DisableSelectButton();
				CurrentDirSelected();
			}

			public void Cancel()
			{
				taskCompletition.SetResult(null);
				proxy.Hide();
			}

			public void Dispose()
			{
				foreach (var entry in cDirEntries) {
					entry.Dispose();
				}

				pathEdit.TextChanged -= PathChanged;
				fileView.ItemSelected -= OnItemSelected;
				fileView.ItemDoubleClicked -= OnItemDoubleClicked;
				selectButton.Released -= SelectButtonPressed;
				backButton.Released -= BackButtonReleased;
				Game.Input.KeyDown -= KeyDown;

				window.RemoveAllChildren();
				window.Remove();
				window.Dispose();
				pathEdit.Dispose();
				selectButton.Dispose();
				fileView.Dispose();
			}

			void BackButtonReleased(ReleasedEventArgs obj)
			{
				Cancel();
			}

			void SelectButtonPressed(ReleasedEventArgs obj)
			{
				if (totalMatch != null) {
					Debug.Assert(SelectEntry(totalMatch), "Select button was enabled with totalMatch an unselectable item");

					Selected(totalMatch.Name, totalMatch.IsDirectory);
				}
				else {
					Debug.Assert(currentDirMatch, "Nothing was there to select even though select button was enabled");

					Selected("", true);
				}
			}

			void KeyDown(KeyDownEventArgs args)
			{
				if (args.Key == Key.Return) {
					//Bare return
					if (args.Qualifiers == 0 && totalMatch != null) {
						currentFileName = totalMatch.Name;
						pathEdit.Text = RelativePath;
					}
					//Ctrl + return
					else if (args.Qualifiers == 2 && totalMatch != null && totalMatch.IsDirectory) {
						pathEdit.Text = Path.Combine(currentDirectory, totalMatch.Name) + Path.DirectorySeparatorChar;
					}
					//Shift + return
					else if (args.Qualifiers == 1 && currentDirectory != "") {
						string nextDirectory = Path.GetDirectoryName(currentDirectory);
						pathEdit.Text = nextDirectory == "" ? "" : nextDirectory + Path.DirectorySeparatorChar;
					}
				}
				

			}

			void PathChanged(TextChangedEventArgs args)
			{
				string oldPath = Path.Combine(currentDirectory, currentFileName);
				string newDirPath = null;
				try {
					string newPath = args.Text;
					newDirPath = newPath == "" ? "" : Path.GetDirectoryName(newPath);
				}
				catch (ArgumentException e) {
					//TODO: Save previous text
					pathEdit.Text = oldPath;
					return;
				}
				catch (PathTooLongException e) {
					pathEdit.Text = oldPath;
					return;
				}
				
				//If directory changed, show the contents of the new directory
				if (newDirPath != currentDirectory) {

					//Load the new directory contents
					if (!LoadDirectory(newDirPath)) {
						//The new directory could not be loaded, revert change
						pathEdit.Text = oldPath;
						return;
					}
				}

				//Path may not represent the current directory, but some files in it
				// Later, after we know the fileName, we check it and set it again if needed
				CurrentDirDeselected();

				string fileName = null;
				try {
					fileName = Path.GetFileName(args.Text);
				}
				catch (ArgumentException e)
				{
					//Hide all items, no item matches invalid fileName
					foreach (var entry in cDirEntries) {
						entry.Text.Visible = false;
					}
					return;
				}

				//At this point we are certain that both dirPath and fileName are correct, set them as current
				currentDirectory = newDirPath;
				currentFileName = fileName;

				//If empty filename, path points to the directory itself
				if (fileName == "") {
					TotalUnmatch();
					CurrentDirSelected();

					foreach (var entry in cDirEntries) {
						entry.Text.Visible = true;
					}
					return;
				}

				//Path points to some files in the directory
				int matches = 0;
				NameTextPair lastMatch = null;
				foreach (var entry in cDirEntries) {
					//Show only the matching files
					bool match = entry.Name.StartsWith(fileName);
					entry.Text.Visible = match;
					if (match) {
						matches++;
						lastMatch = entry;
					}
				}

				//If only one file matches, enable selecting
				if (matches == 1) {
					TotalMatch(lastMatch);
				}
				else {
					TotalUnmatch();
				}
			}

			void OnItemSelected(ItemSelectedEventArgs args)
			{
				string itemText = ((Text) fileView.SelectedItem).Value;
				//Cant select the back link
				if (itemText != "..") {
					pathEdit.Text = Path.Combine(currentDirectory, itemText);
					TotalMatch(cDirEntries[args.Selection]);
				}
				
			}

			void OnItemDoubleClicked(ItemDoubleClickedEventArgs args)
			{
				var entry = cDirEntries.Find((pair) => pair.Text == args.Item);

				if (entry.IsDirectory) {
					//Calls pathChanged, which does the loading logic
					string nextDirectory;
					if (entry.Name == "..") {
						nextDirectory =  Path.GetDirectoryName(currentDirectory);
						if (nextDirectory != "") {
							nextDirectory += Path.DirectorySeparatorChar;
						}
					}
					else {
						nextDirectory = Path.Combine(currentDirectory, entry.Name) + Path.DirectorySeparatorChar;
					}
					//Calls PathChanged
					pathEdit.Text = nextDirectory;
				}
				else  {
					Selected(entry.Name, false);
				}
			}

			void Selected(string name, bool dir)
			{
				taskCompletition.SetResult(new PathResult(baseDir, Path.Combine(currentDirectory, name), dir ? SelectOption.Directory : SelectOption.File));
				proxy.Hide();
			}

			bool LoadDirectory(string relativeDirPath)
			{
				string absoluteDirPath = Path.Combine(baseDir, relativeDirPath);
				var newEntries = new List<NameTextPair>();
				try {
					foreach (var file in MyGame.Files.GetFSEntriesInDirectory(absoluteDirPath, true, false)) {
						newEntries.Add(new NameTextPair(Path.GetFileName(file), false));
					}

					foreach (var directory in MyGame.Files.GetFSEntriesInDirectory(absoluteDirPath, false, true)) {
						newEntries.Add(new NameTextPair(Path.GetFileName(directory), true));
					}
				}
				catch (IOException e) {
					return false;
				}

				currentDirectory = relativeDirPath;
				ClearFileView();

				cDirEntries = newEntries;
				cDirEntries.Sort();

				if (absoluteDirPath != baseDir) {
					cDirEntries.Insert(0, new NameTextPair("..", true));
				}

				foreach (var entry in cDirEntries) {
					fileView.AddItem(entry.Text);
				}
				return true;
			}

			void ClearFileView()
			{
				fileView.RemoveAllItems();
				foreach (var entry in cDirEntries) {
					entry.Dispose();
				}

				cDirEntries = null;
			}

			void TotalMatch(NameTextPair selectedEntry)
			{
				totalMatch = selectedEntry;
				if (SelectEntry(selectedEntry)) {
					EnableSelectButton();
				}
			}

			void TotalUnmatch()
			{
				DisableSelectButton();
				totalMatch = null;
			}

			void CurrentDirSelected()
			{
				if (SelectDirs(selectOptions)) {
					EnableSelectButton();
					currentDirMatch = true;
				}
			}

			void CurrentDirDeselected()
			{
				if (SelectDirs(selectOptions)) {
					DisableSelectButton();
					currentDirMatch = true;
				}
			}

			void EnableSelectButton()
			{
				if (!selectButton.Enabled) {
					selectButton.SetStyleAuto();
					selectButton.Enabled = true;
				}
			}

			void DisableSelectButton()
			{
				if (selectButton.Enabled) {
					selectButton.SetStyle("DisabledButton");
					selectButton.Enabled = false;
				}
				
			}

			bool SelectEntry(NameTextPair entry)
			{
				return entry.IsDirectory ? SelectDirs(selectOptions) : SelectFiles(selectOptions);
			}
		}


		Screen screen;

		public FileSystemBrowsingPopUp(MenuUIManager uiManager)
		{

		}

		/// <summary>
		/// Shows the fileBrowsing screen, enabling user to search the baseDir and its subtree and select a file
		/// or a directory from it.
		/// </summary>
		/// <param name="baseDir"></param>
		/// <param name="selectOption"></param>
		/// <param name="startingRelativePath"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"/>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="PathTooLongException"/>
		public Task<IPathResult> Request(string baseDir, SelectOption selectOption, string startingRelativePath = null)
		{
			if (baseDir == null) {
				throw new ArgumentNullException(nameof(baseDir), "Base directory cannot be null");
			}

			TaskCompletionSource<IPathResult> tcs = new TaskCompletionSource<IPathResult>();

			screen = new Screen(this, baseDir, startingRelativePath, selectOption, tcs);
			return tcs.Task;
		}

		public void Cancel()
		{
			//TODO: throw if not running
			screen?.Cancel();
		}

		void Hide()
		{
			screen.Dispose();
			screen = null;
		}

		static bool SelectFiles(SelectOption selectOption)
		{
			return (selectOption & SelectOption.File) != 0;
		}

		static bool SelectDirs(SelectOption selectOption)
		{
			return (selectOption & SelectOption.Directory) != 0;
		}
	}
}
