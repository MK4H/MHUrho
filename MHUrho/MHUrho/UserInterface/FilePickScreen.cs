using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	abstract class FilePickScreen : MenuScreen {

		protected abstract class FilePickScreenBase : ScreenBase {

			protected struct NameTextPair : IComparable<NameTextPair> {

				public readonly string Name;
				public readonly Text Text;

				public NameTextPair(string name)
				{
					Name = name;
					Text = new Text
					{
						Value = name
					};
					//Text.SetStyle("FileViewText");
					Text.SetStyleAuto();
				}

				public int CompareTo(NameTextPair other)
				{
					return string.Compare(Name, other.Name, StringComparison.CurrentCultureIgnoreCase);
				}
			}

			protected Window Window;
			protected LineEdit LineEdit;
			protected Button DeleteButton;
			protected ListView FileView;
			protected Button BackButton;

			protected List<NameTextPair> Filenames;

			protected string MatchSelected;
			protected string Filename;


			protected FilePickScreenBase(FilePickScreen proxy)
				:base(proxy)
			{
				Filenames = new List<NameTextPair>();
			}

			protected void LoadFileNames(string absolutePath)
			{
				foreach (var file in MyGame.Files.GetFSEntriesInDirectory(MyGame.Files.SaveGameDirAbsolutePath, true, false))
				{
					Filenames.Add(new NameTextPair(Path.GetFileName(file)));
				}

				Filenames.Sort();
			}

			protected void InitUIElements(Window window,
										LineEdit lineEdit,
										Button deleteButton,
										Button backButton,
										ListView fileView)
			{
				this.Window = window;
				this.LineEdit = lineEdit;
				this.DeleteButton = deleteButton;
				this.FileView = fileView;
				this.BackButton = backButton;
				this.Filename = "";

				foreach (var nameText in Filenames)
				{
					FileView.AddItem(nameText.Text);
					nameText.Text.Visible = true;
				}

				MatchSelected = null;

				LineEdit.Text = "";

				Window.Visible = true;

				LineEdit.TextChanged += NameEditTextChanged;
				DeleteButton.Pressed += DeleteButton_Pressed;
				FileView.ItemSelected += OnFileSelected;
				BackButton.Pressed += BackButton_Pressed;
			}

			public override void Dispose()
			{
				LineEdit.TextChanged -= NameEditTextChanged;
				DeleteButton.Pressed -= DeleteButton_Pressed;
				FileView.ItemSelected -= OnFileSelected;
				BackButton.Pressed -= BackButton_Pressed;

				Window.Visible = false;
				if (Filenames != null)
				{
					foreach (var item in Filenames)
					{
						item.Text.Remove();
						item.Text.Dispose();
					}
					Filenames = null;
				}

				MatchSelected = null;

				FileView.RemoveAllItems();

				Window.RemoveAllChildren();
				Window.Remove();

				Window.Dispose();
				LineEdit.Dispose();
				DeleteButton.Dispose();
				FileView.Dispose();
				BackButton.Dispose();
			}


			protected virtual void TotalMatchSelected(string newMatchSelected)
			{
				DeleteButton.Enabled = true;
				MatchSelected = newMatchSelected;
			}

			protected virtual void TotalMatchDeselected()
			{
				DeleteButton.Enabled = false;
				MatchSelected = null;
			}

			/// <summary>
			/// Checks the new filename provided by the user and if it is valid, sets it as the new <see cref="Filename"/>
			///
			/// Also displays the files that match the current filename and if only one file matches, invokes the <see cref="TotalMatchSelected(string)"/>
			/// If previously <see cref="MatchSelected"/> was not null, and now there is not just one match, invokes <see cref="TotalMatchDeselected"/>
			/// </summary>
			/// <param name="args"></param>
			protected virtual void NameEditTextChanged(TextChangedEventArgs args)
			{
				string newText = args.Text;
				string newMatchSelected = null;


				if (!IsValidFilename(newText)) {
					//If the new filename is invalid, leave the last valid filename
					// the displayed files match the last valid filename already, so no need to check them again
					((LineEdit) args.Element).Text = Filename;
				}
				else {
					Filename = newText;
					//Display the files matching the new filename
					foreach (var nameText in Filenames)
					{
						bool visible = nameText.Name.StartsWith(Filename, StringComparison.CurrentCultureIgnoreCase);
						nameText.Text.Visible = visible;

						if (visible && Filename.Equals(nameText.Name, StringComparison.CurrentCultureIgnoreCase))
						{
							newMatchSelected = Filename;
						}
					}
				}

				if (MatchSelected == null && newMatchSelected != null)
				{
					TotalMatchSelected(newMatchSelected);
				}
				else if (MatchSelected != null && newMatchSelected == null)
				{
					TotalMatchDeselected();
				}
			}

			protected bool IsValidFilename(string filename)
			{
				return filename.IndexOfAny(Path.GetInvalidFileNameChars()) == -1;
			}

			void DeleteButton_Pressed(PressedEventArgs args)
			{
				if (MatchSelected == null) return;

				DisableInput();
				MenuUIManager.ConfirmationPopUp
							.RequestConfirmation("Deleting file",
												$"Do you really want to delete the file \"{MatchSelected}\"?")
							.ContinueWith(DeleteFile, TaskScheduler.FromCurrentSynchronizationContext());


			}

			void DeleteFile(Task<bool> confirmed)
			{
				EnableInput();
				if (!confirmed.Result) {
					return;
				}

				MyGame.Files.DeleteDynamicFile(Path.Combine(MyGame.Files.SaveGameDirPath, MatchSelected));
				int index = Filenames.FindIndex((pair) => pair.Name.Equals(MatchSelected, StringComparison.CurrentCultureIgnoreCase));

				FileView.RemoveItem(Filenames[index].Text);
				Filenames[index].Text.Dispose();
				Filenames.RemoveAt(index);

				MatchSelected = null;
				TotalMatchDeselected();
				LineEdit.Text = "";
			}

			void BackButton_Pressed(PressedEventArgs args)
			{
				MenuUIManager.SwitchBack();
			}

			

			void OnFileSelected(ItemSelectedEventArgs args)
			{
				LineEdit.Text = ((Text)FileView.SelectedItem).Value;
				TotalMatchSelected(LineEdit.Text);
			}
		}

		protected FilePickScreen(MenuUIManager menuUIManager)
			: base(menuUIManager)
		{ }
	}
   
}
