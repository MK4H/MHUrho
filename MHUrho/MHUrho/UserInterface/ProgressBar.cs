using System;
using System.Collections.Generic;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class ProgressBar {
		public bool Visible {
			get => progressBar.Visible;
			set {
				if (value) {
					Show();
				}
				else {
					Hide();
				}
			}
		
		}

		readonly BorderImage progressBar;
		readonly BorderImage progressBarKnob;

		float value;

		public ProgressBar(UIElement progressBarElement)
		{
			if (progressBarElement == null) {
				throw new ArgumentNullException(nameof(progressBarElement));
			}

			this.progressBar = (BorderImage)progressBarElement;
			this.progressBarKnob = (BorderImage)progressBar.GetChild("ProgressBarKnob");
		}


		public void SetValue(float newValue)
		{
			value = Urho.MathHelper.Clamp(newValue, 0, 100);
			CorrectKnobSize();
		}

		public void ChangeValue(float change)
		{
			SetValue(value + change);
		}

		public void Show()
		{
			progressBar.Visible = true;
		}

		public void Hide()
		{
			progressBar.Visible = false;
		}

		void CorrectKnobSize()
		{
			progressBarKnob.MinWidth = (int)((progressBar.Width / 100.0f) * value);
		}
    }
}
