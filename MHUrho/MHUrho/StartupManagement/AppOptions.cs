using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;
using Urho;

namespace MHUrho.StartupManagement
{
	[DataContract]
    public class AppOptions
    {
		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 0)]
		public float UnitDrawDistance { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 1)]
		public float ProjectileDrawDistance { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 2)]
		public float TerrainDrawDistance { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 3)]
		public IntVector2 Resolution { get; set; }

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

		//TODO: Dont know what this does
		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 12)]
		public int RefreshRateCap { get; set; }

		[DataMember(EmitDefaultValue = true, IsRequired = true, Order = 13)]
		public bool DebugHUD { get; set; }
		//TODO: Other things



		public static AppOptions LoadFrom(Stream stream)
		{

			var serializer = new DataContractSerializer(typeof(AppOptions));
			
			//TODO: Catch
			AppOptions newOptions = (AppOptions)serializer.ReadObject(stream);
			return newOptions;
		}

		public void SaveTo(Stream stream)
		{
			XmlWriterSettings settings = new XmlWriterSettings
										{
											Indent = true,
											CloseOutput = true
										};
			using (XmlWriter writer = XmlWriter.Create(stream, settings)) {
				var serializer = new DataContractSerializer(typeof(AppOptions));
				serializer.WriteObject(writer, this);
			}
		}

		public static AppOptions GetDefaultAppOptions()
		{
			return new AppOptions()
					{
						UnitDrawDistance = 200,
						ProjectileDrawDistance = 200,
						TerrainDrawDistance = 200,
						Resolution = new IntVector2(1024,768),
						Fullscreen = false,
						Borderless = false,
						Resizable = false,
						HighDPI = false,
						VSync = false,
						TripleBuffer = false,
						Multisample = 1,
						Monitor = 0,
						RefreshRateCap = 0,
						DebugHUD = true
					};
		}

	}
}
