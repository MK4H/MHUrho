using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Helpers;
using MHUrho.Packaging;

namespace MHUrho.EntityInfo
{
    public class PlayerInsignia {
		public static IReadOnlyList<PlayerInsignia> Insignias;

		public static void InitInsignias(PackageManager packageManager)
		{
			if (Insignias != null) {
				return;
			}

			const float numPlayers = 8.0f;
			const float numHealthBarImages = 21.0f;
			Material healthBarMaterial = packageManager.GetMaterial("Materials/HealthBarMat.xml");
			Insignias = new List<PlayerInsignia>
						{
							new PlayerInsignia(Color.FromByteFormat(224, 23, 23, 255), 
												new IntRect(0, 0, 50, 50),
												healthBarMaterial,
												new Rect(new Vector2(0 / numPlayers, 0), new Vector2(1 / numPlayers, 1 / numHealthBarImages)),
											    5),
							new PlayerInsignia(Color.FromByteFormat(239, 129, 10, 255),
												new IntRect(0, 50, 50, 100),
												healthBarMaterial,
												new Rect(new Vector2(1 / numPlayers,0), new Vector2(2 / numPlayers, 1 / numHealthBarImages)),
												5),
							new PlayerInsignia(Color.FromByteFormat(10, 75, 239, 255), 
												new IntRect(0, 100, 50, 150),
												healthBarMaterial,
												new Rect(new Vector2(2 / numPlayers,0), new Vector2(3 / numPlayers, 1 / numHealthBarImages)),
												5),
							new PlayerInsignia(Color.FromByteFormat(62, 62, 63, 255), 
												new IntRect(0, 150, 50, 200),
												healthBarMaterial,
												new Rect(new Vector2(3 / numPlayers,0), new Vector2(4 / numPlayers, 1 / numHealthBarImages)),
												5),
							new PlayerInsignia(Color.FromByteFormat(192, 0, 255, 255), 
												new IntRect(0, 200, 50, 250),
												healthBarMaterial,
												new Rect(new Vector2(4 / numPlayers,0), new Vector2(5 / numPlayers, 1 / numHealthBarImages)),
												5),
							new PlayerInsignia(Color.FromByteFormat(17, 221, 239, 255),
												new IntRect(0, 250, 50, 300),
												healthBarMaterial,
												new Rect(new Vector2(5 / numPlayers,0), new Vector2(6 / numPlayers, 1 / numHealthBarImages)),
												5),
							new PlayerInsignia(Color.FromByteFormat(255, 236, 19, 255),
												new IntRect(0, 300, 50, 350),
												healthBarMaterial,
												new Rect(new Vector2(6 / numPlayers,0), new Vector2(7 / numPlayers, 1 / numHealthBarImages)),
												5),
							new PlayerInsignia(Color.FromByteFormat(19, 255, 36, 255), 
												new IntRect(0, 350, 50, 400),
												healthBarMaterial,
												new Rect(new Vector2(7 / numPlayers,0), new Vector2(8 / numPlayers, 1 / numHealthBarImages)),
												5)
						};
		}

		public Color Color { get; private set; }

		public IntRect ShieldIcon { get; private set; }

		public Material HealthBarMat { get; private set; }

		public Rect HealthBarFullUv { get; private set; }

		public int HealthBarStepSize { get; private set; }

		public int ID => Insignias.IndexOf(this);

		protected PlayerInsignia(Color color, 
								IntRect shieldIcon, 
								Material healthBarMat,
								Rect healthBarFullUv,
								int healthBarStepSize)
		{
			this.Color = color;
			this.ShieldIcon = shieldIcon;
			this.HealthBarMat = healthBarMat;
			this.HealthBarFullUv = healthBarFullUv;
			this.HealthBarStepSize = healthBarStepSize;
		}

		public static PlayerInsignia GetInsignia(int ID)
		{
			if (Insignias == null) {
				throw new InvalidOperationException("Insignias were not initialized");
			}
			return Insignias[ID];
		}
	}
}
