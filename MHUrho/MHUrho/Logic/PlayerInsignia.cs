using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Helpers;

namespace MHUrho.Logic
{
    public class PlayerInsignia {
		public static IReadOnlyList<PlayerInsignia> Insignias
			= new List<PlayerInsignia>
			{
				new PlayerInsignia(Color.FromByteFormat(224, 23, 23, 255), new IntRect(0, 0, 50, 50)),
				new PlayerInsignia(Color.FromByteFormat(239, 129, 10, 255), new IntRect(0, 50, 50, 100)),
				new PlayerInsignia(Color.FromByteFormat(10, 75, 239, 255), new IntRect(0, 100, 50, 150)),
				new PlayerInsignia(Color.FromByteFormat(62, 62, 63, 255), new IntRect(0, 150, 50, 200)),
				new PlayerInsignia(Color.FromByteFormat(192, 0, 255, 255), new IntRect(0, 200, 50, 250)),
				new PlayerInsignia(Color.FromByteFormat(17, 221, 239, 255), new IntRect(0, 250, 50, 300)),
				new PlayerInsignia(Color.FromByteFormat(255, 236, 19, 255), new IntRect(0, 300, 50, 350)),
				new PlayerInsignia(Color.FromByteFormat(19, 255, 36, 255), new IntRect(0, 350, 50, 400)),
			};

		public Color Color { get; private set; }

		public IntRect ShieldIcon { get; private set; }

		public int ID => Insignias.IndexOf(this);

		protected PlayerInsignia(Color color, IntRect shieldIcon)
		{
			this.Color = color;
			this.ShieldIcon = shieldIcon;
		}

		public static PlayerInsignia GetInsignia(int ID)
		{
			return Insignias[ID];
		}
	}
}
