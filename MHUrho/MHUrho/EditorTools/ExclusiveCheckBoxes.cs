using System;
using System.Collections.Generic;
using System.Text;
using Urho.Gui;

namespace MHUrho.EditorTools
{
	delegate void OnSelectedDelegate(CheckBox newSelected, CheckBox oldSelected);

    class ExclusiveCheckBoxes : IDisposable
    {
		public IEnumerable<CheckBox> CheckBoxes => checkBoxes;

		public CheckBox Selected { get; private set; }

		public event OnSelectedDelegate SelectedChanged;

		bool pvisible;

		public bool Visible {
			get => pvisible;
			set {
				if (value) {
					Show();
				}
				else {
					Hide();
				}
				pvisible = value;
			}
		}

		readonly List<CheckBox> checkBoxes;

		public ExclusiveCheckBoxes()
		{
			checkBoxes = new List<CheckBox>();
		}

		public void AddCheckBox(CheckBox checkBox)
		{
			checkBoxes.Add(checkBox);
			checkBox.Toggled += OnToggled;
		}

		public void RemoveCheckBox(CheckBox checkBox)
		{
			if (!checkBoxes.Remove(checkBox)) {
				throw new ArgumentException("Provided checkbox was not registered", nameof(checkBox));
			}

			checkBox.Toggled -= OnToggled;
			
		}

		public void Deselect()
		{
			if (Selected != null) {
				Selected.Checked = false;
			}
		}

		public void Show()
		{
			if (Visible) return;

			foreach (var checkBox in checkBoxes) {
				checkBox.Visible = true;
			}
		}

		public void Hide()
		{
			if (!Visible) return;

			foreach (var checkBox in checkBoxes) {
				checkBox.Visible = false;
			}
		}

		void OnToggled(ToggledEventArgs e)
		{
			CheckBox oldSelected = Selected;
			if (e.State) {
				if (Selected != null) {
					Selected.Checked = false;
				}
				Selected = (CheckBox)e.Element;	
			}
			else {
				Selected = null;				
			}
			SelectedChanged?.Invoke(Selected, oldSelected);
		}

		public void Dispose()
		{
			Selected = null;

			foreach (var checkBox in checkBoxes) {
				checkBox.Toggled -= OnToggled;
				checkBox.Dispose();
			}

			checkBoxes.Clear();
		}
	}
}
