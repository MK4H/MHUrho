using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Urho;

namespace MHUrho.StartupManagement
{
	[DataContract]
    public class Resolution
    {
		[DataMember(EmitDefaultValue = true, IsRequired = true)]
		public IntVector2 Value { get; private set; }

		public int Width => Value.X;

		public int Height => Value.Y;

		public Resolution()
		{

		}

		public Resolution(int width, int height)
		{
			Value = new IntVector2(width, height);
		}

		public override string ToString()
		{
			return $"{Width}x{Height}";
		}

		public override bool Equals(object obj)
		{
			Resolution other = obj as Resolution;
			if (other == null) {
				return false;
			}

			return Value == other.Value;
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
}
