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

		public static int InsigniaCount => 9;

		public static PlayerInsignia NeutralPlayerInsignia => Insignias[0];

		public Color Color { get; private set; }

		public Texture ShieldTexture { get; private set; }

		public IntRect ShieldRectangle { get; private set; }

		public Material HealthBarMat { get; private set; }

		public Rect HealthBarFullUv { get; private set; }

		public int HealthBarStepSize { get; private set; }

		public int Index => Insignias.IndexOf(this);

		protected PlayerInsignia(Color color,
								Texture shieldTexture,
								IntRect shieldRectangle, 
								Material healthBarMat,
								Rect healthBarFullUv,
								int healthBarStepSize)
		{
			this.Color = color;
			this.ShieldTexture = shieldTexture;
			this.ShieldRectangle = shieldRectangle;
			this.HealthBarMat = healthBarMat;
			this.HealthBarFullUv = healthBarFullUv;
			this.HealthBarStepSize = healthBarStepSize;
		}

		public static void InitInsignias(PackageManager packageManager)
		{
			if (Insignias != null)
			{
				return;
			}

			const float numPlayers = 9.0f;
			const float numHealthBarImages = 21.0f;
			Material healthBarMaterial = packageManager.GetMaterial("Materials/HealthBarMat.xml");
			Texture shieldTexture = packageManager.GetTexture2D("Textures/PlayerIconShields.png");
			Insignias = new List<PlayerInsignia>
						{
							new PlayerInsignia(Color.FromByteFormat(90, 90, 90, 255),
												shieldTexture,
												new IntRect(0, 0, 50, 50),
												healthBarMaterial,
												new Rect(new Vector2(0 / numPlayers, 0), new Vector2(1 / numPlayers, 1 / numHealthBarImages)),
												5),
							new PlayerInsignia(Color.FromByteFormat(224, 23, 23, 255),
												shieldTexture,
												new IntRect(0, 50, 50, 100),
												healthBarMaterial,
												new Rect(new Vector2(1 / numPlayers, 0), new Vector2(2 / numPlayers, 1 / numHealthBarImages)),
												5),
							new PlayerInsignia(Color.FromByteFormat(239, 129, 10, 255),
												shieldTexture,
												new IntRect(0, 100, 50, 150),
												healthBarMaterial,
												new Rect(new Vector2(2 / numPlayers,0), new Vector2(3 / numPlayers, 1 / numHealthBarImages)),
												5),
							new PlayerInsignia(Color.FromByteFormat(10, 75, 239, 255),
												shieldTexture,
												new IntRect(0, 150, 50, 200),
												healthBarMaterial,
												new Rect(new Vector2(3 / numPlayers,0), new Vector2(4 / numPlayers, 1 / numHealthBarImages)),
												5),
							new PlayerInsignia(Color.FromByteFormat(0, 0, 0, 255),
												shieldTexture,
												new IntRect(0, 200, 50, 250),
												healthBarMaterial,
												new Rect(new Vector2(4 / numPlayers,0), new Vector2(5 / numPlayers, 1 / numHealthBarImages)),
												5),
							new PlayerInsignia(Color.FromByteFormat(192, 0, 255, 255),
												shieldTexture,
												new IntRect(0, 250, 50, 300),
												healthBarMaterial,
												new Rect(new Vector2(5 / numPlayers,0), new Vector2(6 / numPlayers, 1 / numHealthBarImages)),
												5),
							new PlayerInsignia(Color.FromByteFormat(17, 221, 239, 255),
												shieldTexture,
												new IntRect(0, 300, 50, 350),
												healthBarMaterial,
												new Rect(new Vector2(6 / numPlayers,0), new Vector2(7 / numPlayers, 1 / numHealthBarImages)),
												5),
							new PlayerInsignia(Color.FromByteFormat(255, 236, 19, 255),
												shieldTexture,
												new IntRect(0, 350, 50, 400),
												healthBarMaterial,
												new Rect(new Vector2(7 / numPlayers,0), new Vector2(8 / numPlayers, 1 / numHealthBarImages)),
												5),
							new PlayerInsignia(Color.FromByteFormat(19, 255, 36, 255),
												shieldTexture,
												new IntRect(0, 400, 50, 450),
												healthBarMaterial,
												new Rect(new Vector2(8 / numPlayers,0), new Vector2(9 / numPlayers, 1 / numHealthBarImages)),
												5)
						};
		}


		public static PlayerInsignia GetInsignia(int index)
		{
			if (Insignias == null)
			{
				throw new InvalidOperationException("Insignias were not initialized");
			}

			if (index < 0 || (InsigniaCount - 1) < index)
			{
				throw new ArgumentOutOfRangeException(nameof(index), index, $"Only {InsigniaCount} insignias are supported");
			}

			return Insignias[index];
		}
	}

	public class InsigniaGetter {
		public int NeutralPlayerIndex => 0;

		readonly List<int> freeIndicies;

		public InsigniaGetter()
		{
			PlayerInsignia.InitInsignias(PackageManager.Instance);
			freeIndicies = new List<int>();
			for (int i = 0; i < PlayerInsignia.InsigniaCount; i++) {
				freeIndicies.Add(i);
			}
		}

		public PlayerInsignia GetUnusedInsignia(int index)
		{
			PlayerInsignia insignia = PlayerInsignia.GetInsignia(index);

			if (!freeIndicies.Remove(index)) {
				throw new ArgumentException("Index was already in use");
			}

			return insignia;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="insignia"></param>
		/// <returns>Returns the argument <paramref name="insignia"/></returns>
		/// <exception cref="ArgumentException">Thrown when the insignia is already in use</exception>
		public PlayerInsignia MarkUsed(PlayerInsignia insignia)
		{
			if (!freeIndicies.Remove(insignia.Index)) {
				throw new ArgumentException("Insignia was already in use");
			}

			return insignia;
		}

		public PlayerInsignia GetNextUnusedInsignia()
		{
			PlayerInsignia insignia = null;

			for (int i = 0; i < freeIndicies.Count; i++) {
				int index = freeIndicies[i];
				if (index != NeutralPlayerIndex) {
					freeIndicies.RemoveAt(i);
					insignia = PlayerInsignia.GetInsignia(index);
					break;
				}
			}

			return insignia;
		}
	}
}
