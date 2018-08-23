using System;
using System.Collections.Generic;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    abstract class SliderLineEditCombo : IDisposable {
		public event Action<float> ValueChanged;
		public abstract float Value { get; set; }

		protected virtual void OnValueChanged(float value)
		{
			ValueChanged?.Invoke(value);
		}

		public virtual void Dispose()
		{
			ValueChanged = null;
		}
	}

	class PercentageSlLeCombo : SliderLineEditCombo {



		public override float Value {
			get => GetRealValue(slider.Value);
			set {
				int newPercentage = Math.Max(0, Math.Min(100, GetPercentageFromRealValue(value)));
				slider.Value = GetSliderValue(newPercentage);
			}
		} 

		const float SliderRange = 10.0f;

		Slider slider;
		LineEdit lineEdit;

		Func<float> maxValueGetter;

		public PercentageSlLeCombo(Slider slider, LineEdit lineEdit, Func<float> maxValueGetter)
		{
			this.slider = slider;
			this.lineEdit = lineEdit;
			this.maxValueGetter = maxValueGetter;

			slider.Range = SliderRange;

			slider.SliderChanged += SliderChanged;

			lineEdit.TextChanged += EditTextChanged;

			lineEdit.TextFinished += EditTextFinished;
		}

		public override void Dispose()
		{
			base.Dispose();
			slider.SliderChanged -= SliderChanged;

			lineEdit.TextChanged -= EditTextChanged;

			lineEdit.TextFinished -= EditTextFinished;

			slider.Dispose();
			lineEdit.Dispose();
			maxValueGetter = null;
		}

		void SliderChanged(SliderChangedEventArgs args)
		{
			int percentageValue = GetPercentageFromSlider(args.Value);

			lineEdit.Text = percentageValue.ToString();
			slider.Value = GetSliderValue(percentageValue);

			OnValueChanged(GetRealValue(slider.Value));
		}

		void EditTextChanged(TextChangedEventArgs args)
		{
			if ((!int.TryParse(args.Text, out int value) || value < 0 || 100 < value) && args.Text != "") {
				((LineEdit)args.Element).Text = GetPercentageFromSlider(slider.Value).ToString();
			}

			OnValueChanged(GetRealValue(slider.Value));
		}

		void EditTextFinished(TextFinishedEventArgs args)
		{
			if (!int.TryParse(args.Text, out int value) || value < 0 || 100 < value) {
				if (args.Text == "") {
					slider.Value = GetSliderValue(0);
					((LineEdit)args.Element).Text = 0.ToString();
				}
				else {
					((LineEdit)args.Element).Text = GetPercentageFromSlider(slider.Value).ToString();
				}
			}
			else {
				slider.Value = GetSliderValue(value);
			}

			OnValueChanged(GetRealValue(slider.Value));
		}

		int GetPercentageFromSlider(float sliderValue)
		{
			return (int)Math.Round(sliderValue * (100 / SliderRange));
		}

		int GetPercentageFromRealValue(float realValue)
		{
			return (int)Math.Round((realValue / maxValueGetter()) * 100);
		}

		float GetSliderValue(int percentage)
		{
			return (percentage / 100.0f) * SliderRange;
		}

		float GetRealValue(float sliderValue)
		{
			return maxValueGetter() * (GetPercentageFromSlider(sliderValue) / 100.0f);
		}
	}

	class ValueSlLeCombo : SliderLineEditCombo {

		public override float Value {
			get => GetValue(slider.Value);
			set {
				int newValue = (int)Math.Round(Math.Max(minValue, Math.Min(maxValue, value)));
				slider.Value = GetSliderValue(newValue);
			}
		} 

		Slider slider;
		LineEdit lineEdit;

		int minValue;
		int maxValue;

		public ValueSlLeCombo(Slider slider, LineEdit lineEdit, int minValue, int maxValue)
		{
			this.slider = slider;
			this.lineEdit = lineEdit;
			this.minValue = minValue;
			this.maxValue = maxValue;

			//TODO: Check that it is not negative
			slider.Range = maxValue - minValue;

			slider.SliderChanged += SliderChanged;

			lineEdit.TextChanged += EditTextChanged;

			lineEdit.TextFinished += EditTextFinished;
		}

		public override void Dispose()
		{
			base.Dispose();


			slider.SliderChanged -= SliderChanged;

			lineEdit.TextChanged -= EditTextChanged;

			lineEdit.TextFinished -= EditTextFinished;
		}

		void SliderChanged(SliderChangedEventArgs args)
		{
			int value = GetValue(args.Value);
			lineEdit.Text = value.ToString();
			slider.Value = GetSliderValue(value);

			OnValueChanged(value);
		}

		void EditTextChanged(TextChangedEventArgs args)
		{
			if ((!int.TryParse(args.Text, out int value) || value < minValue || maxValue < value) && args.Text != "") {
				((LineEdit)args.Element).Text = GetValue(slider.Value).ToString();
			}

			OnValueChanged(GetValue(slider.Value));
		}

		void EditTextFinished(TextFinishedEventArgs args)
		{
			if (!int.TryParse(args.Text, out int value) || value < minValue || maxValue < value) {
				if (args.Text == "") {
					slider.Value = minValue;
					((LineEdit)args.Element).Text = minValue.ToString();
				}
				else {
					((LineEdit)args.Element).Text = GetValue(slider.Value).ToString();
				}
			}
			else {
				slider.Value = GetSliderValue(value);
			}

			OnValueChanged(GetValue(slider.Value));
		}

		int GetValue(float sliderValue)
		{
			return minValue + (int)Math.Round(sliderValue);
		}

		float GetSliderValue(int value)
		{
			return value - minValue;
		}
	}
}
