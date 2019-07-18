using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;
using MHUrho.PathFinding;
using Urho;

namespace MHUrho.StartupManagement
{
	[DataContract]
	public class AppConfig {
		//NOTE: Could store this in the config file as well
		public int MaxDrawDistance => 1000;
		public int MinDrawDistance => 100;

		public float MaxCameraScrollSensitivity => 20;

		public float MaxCameraRotationSensitivity => 20;

		public float MaxMouseRotationSensitivity => 0.5f;
		public float MaxZoomSensitivity => 10.0f;


		static readonly List<Resolution> SupportedResolutionsStatic  = new List<Resolution>
																		{
																			new Resolution(1024,768),
																		};

		static readonly List<Visualization> SupportedPathFindingVisualizationsStatic =
			new List<Visualization> {Visualization.None, Visualization.TouchedNodes, Visualization.FinalPath};

		public IReadOnlyList<Resolution> SupportedResolutions => SupportedResolutionsStatic;

		public IReadOnlyList<Visualization> SupportedPathFindingVisualizations =>
			SupportedPathFindingVisualizationsStatic;

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 0)]
		public float UnitDrawDistance { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 1)]
		public float ProjectileDrawDistance { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 2)]
		public float TerrainDrawDistance { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 3)]
		public Resolution Resolution { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 4)]
		public bool Fullscreen { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 5)]
		public bool Borderless { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 6)]
		public bool Resizable { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 7)]
		public bool HighDPI { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 8)]
		public bool VSync { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 9)]
		public bool TripleBuffer { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 10)]
		public int Multisample { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 11)]
		public int Monitor { get; set; }

		//NOTE: Dont know what this does
		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 12)]
		public int RefreshRateCap { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 13)]
		public bool DebugHUD { get; set; }
		//FUTURE: Other things

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 14)]
		public float CameraScrollSensitivity { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 15)]
		public float CameraRotationSensitivity { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 16)]
		public float MouseRotationSensitivity { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 17)]
		public float ZoomSensitivity { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 18)]
		public bool MouseBorderCameraMovement { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 20)]
		public Visualization PathFindingVisualization { get; set; }

		public static AppConfig LoadFrom(Stream stream)
		{

			var serializer = new DataContractSerializer(typeof(AppConfig));
			
			//NOTE: There is no point in catching the error, there is no way we can start 
			// without settings.
			AppConfig newConfig = (AppConfig)serializer.ReadObject(stream);
			return newConfig;
		}

		public void Reload(FileManager files)
		{
			using (Stream configFile =
				files.OpenDynamicFile(files.ConfigFilePath, System.IO.FileMode.Open, FileAccess.Read)) {
				AppConfig reloadedConfig = LoadFrom(configFile);
				Copy(reloadedConfig);
			}
		}

		public void Save(FileManager files)
		{
			using (Stream configFile =
				files.OpenDynamicFile(files.ConfigFilePath, System.IO.FileMode.Truncate, FileAccess.Write)) {
				SaveTo(configFile);
			}		
		}

		public void SaveTo(Stream stream)
		{
			XmlWriterSettings settings = new XmlWriterSettings
										{
											Indent = true,
											CloseOutput = true
										};
			using (XmlWriter writer = XmlWriter.Create(stream, settings)) {
				var serializer = new DataContractSerializer(typeof(AppConfig));
				serializer.WriteObject(writer, this);
			}
		}

		public void SetGraphicsMode(Graphics graphics)
		{
			graphics.SetMode(Resolution.Width,
							Resolution.Height,
							Fullscreen,
							Borderless,
							Resizable,
							HighDPI,
							VSync,
							TripleBuffer,
							Multisample,
							Monitor,
							RefreshRateCap);
		}

		public static AppConfig GetDefaultAppOptions()
		{
			return new AppConfig()
					{
						UnitDrawDistance = 200,
						ProjectileDrawDistance = 200,
						TerrainDrawDistance = 200,
						Resolution = new Resolution(1024,768),
						Fullscreen = false,
						Borderless = false,
						Resizable = false,
						HighDPI = false,
						VSync = false,
						TripleBuffer = false,
						Multisample = 1,
						Monitor = 0,
						RefreshRateCap = 0,
						DebugHUD = true,
						CameraScrollSensitivity = 10.0f,
						CameraRotationSensitivity = 10.0f,
						MouseRotationSensitivity = 0.5f,
						ZoomSensitivity = 10.0f,
						MouseBorderCameraMovement = true,
						PathFindingVisualization = Visualization.None
					};
		}


		void Copy(AppConfig other)
		{
			UnitDrawDistance = other.UnitDrawDistance;

			ProjectileDrawDistance = other.ProjectileDrawDistance;

			TerrainDrawDistance = other.TerrainDrawDistance;

			Resolution = other.Resolution;

			Fullscreen = other.Fullscreen;

			Borderless = other.Borderless;

			Resizable = other.Resizable;

			HighDPI = other.HighDPI;

			VSync = other.VSync;

			TripleBuffer = other.TripleBuffer;

			Multisample = other.Multisample;

			Monitor = other.Monitor;

			RefreshRateCap = other.RefreshRateCap;

			DebugHUD = other.DebugHUD;

			CameraScrollSensitivity = other.CameraScrollSensitivity;

			CameraRotationSensitivity = other.CameraRotationSensitivity;

			MouseRotationSensitivity = other.MouseRotationSensitivity;

			ZoomSensitivity = other.ZoomSensitivity;

			MouseBorderCameraMovement = other.MouseBorderCameraMovement;

			PathFindingVisualization = other.PathFindingVisualization;

		}
	}
}
