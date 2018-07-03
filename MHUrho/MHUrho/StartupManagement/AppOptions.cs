using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Urho;

namespace MHUrho.StartupManagement
{
    public class AppOptions
    {
		class Parser {

			const int BufferSize = 4096;

			StreamReader reader;
			AppOptions options;

			readonly char[] buffer = new char[BufferSize];

			char CurrentChar => buffer[index];

			int index;
			int filledIndex;

			bool endOfFile;

			public void ParseFrom(Stream stream)
			{
				//TODO: Encoding
				reader = new StreamReader(stream);

				options.UnitRenderDistance = GetUnitRenderDistance();
				options.ProjectileRenderDistance = GetProjectileRenderDistance();
				options.TerrainRenderDistance = GetTerrainRenderDistance();
				options.Resolution = GetResolution();
				options.Fullscreen = GetFullscreen();
			}

			float GetUnitRenderDistance()
			{

				CheckName("UnitRenderDistance");
				CheckAssign();

				if (TryGetNextFloat(out float value)) {
					return value;
				}
				//TODO: Exception
				throw new Exception("Invalid config file");
			}

			float GetProjectileRenderDistance()
			{

				CheckName("ProjectileRenderDistance");
				CheckAssign();

				if (TryGetNextFloat(out float value)) {
					return value;
				}

				//TODO: Exception
				throw new Exception("Invalid config file");
			}

			float GetTerrainRenderDistance()
			{
				CheckName("TerrainRenderDistance");
				CheckAssign();

				if (TryGetNextFloat(out float value)) {
					return value;
				}
				//TODO: Exception
				throw new Exception("Invalid config file");
			}

			IntVector2 GetResolution()
			{
				CheckName("Resolution");
				CheckAssign();

				if (TryGetNextInt(out int width) && TryGetNextInt(out int height)) {
					return new IntVector2(width, height);
				}

				//TODO: Exception
				throw new Exception("Invalid config file");
			}

			bool GetFullscreen()
			{
				CheckName("Fullscreen");
				CheckAssign();

				if (TryGetNextBool(out bool value)) {
					return value;
				}

				//TODO: Exception
				throw new Exception("Invalid config file");
			}

			void CheckName(string name)
			{
				if (GetNextToken(char.IsLetter) != name) {
					//TODO: Exception
					throw new Exception("Invalid config file");
				}
			}

			void CheckAssign()
			{
				if (GetNextToken() != "=") {
					//TODO: Exception
					throw new Exception("Invalid config file");
				}
			}

			bool TryGetNextFloat(out float value)
			{
				string valueString;
				if ((valueString = GetNextToken()) != "" && float.TryParse(valueString, out value)) {
					return true;
				}

				value = float.NaN;
				return false;
			}

			bool TryGetNextInt(out int value)
			{
				string valueString;
				if ((valueString = GetNextToken()) != "" && int.TryParse(valueString, out value)) {
					return true;
				}

				value = -1;
				return false;
			}

			bool TryGetNextBool(out bool value)
			{
				string valueString;
				if ((valueString = GetNextToken()) != "" && bool.TryParse(valueString, out value)) {
					return true;
				}

				value = false;
				return false;
			}

			string GetNextToken(Predicate<char> check = null)
			{
				SkipWhitespace();
				StringBuilder stringBuilder = new StringBuilder();
				while (MoveNextChar() && !char.IsWhiteSpace(CurrentChar)) {
					//If CurrentChar does not pass the aditional check
					if (!(check?.Invoke(CurrentChar) ?? true)) {
						break;
					}

					stringBuilder.Append(CurrentChar);
				}

				SkipWhitespace();
				return stringBuilder.ToString();
			}

			void SkipWhitespace()
			{
				while (MoveNextChar() && char.IsWhiteSpace(CurrentChar)) {

				}
			}

			bool MoveNextChar()
			{
				if (++index < filledIndex) {
					return true;
				}

				filledIndex = reader.Read(buffer, 0, BufferSize);

				if (filledIndex != 0) return true;

				endOfFile = true;
				return false;
			}

			public Parser(AppOptions toFill)
			{
				this.options = toFill;
			}
		}

		const string UnitRenderDistanceTag = "UnitRenderDistance";
		public float UnitRenderDistance { get; set; }

		const string ProjectileRenderDistanceTag = "UnitRenderDistance";
		public float ProjectileRenderDistance { get; set; }

		public float TerrainRenderDistance { get; set; }

		public IntVector2 Resolution { get; set; }

		public bool Fullscreen { get; set; }

		public bool DebugHUD { get; set; }
		//TODO: Other things

		public static AppOptions LoadFrom(Stream stream)
		{
			var newOptions = new AppOptions();
			var parser = new Parser(newOptions);

			parser.ParseFrom(stream);

			parser.Dispose();

			return newOptions;
		}

		public void SaveTo(Stream stream)
		{
			var writer = new StreamWriter(stream);

			writer.WriteLine($"")
		}

    }
}
