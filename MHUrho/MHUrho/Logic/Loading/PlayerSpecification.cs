using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MHUrho.EntityInfo;

namespace MHUrho.Logic
{
	public class PlayerInfo {
		public bool IsNeutral { get; private set; }

		public bool IsHuman { get; private set; }

		public PlayerType PlayerType { get; private set; }

		public int TeamID { get; private set; }

		public PlayerInsignia Insignia { get; private set; }

		protected PlayerInfo(PlayerType playerType, int teamID, PlayerInsignia insignia, bool isHuman, bool isNeutral)
		{
			this.PlayerType = playerType;
			this.TeamID = teamID;
			this.Insignia = insignia;
			this.IsHuman = isHuman;
			this.IsNeutral = isNeutral;
		}

		public static PlayerInfo CreateNeutralPlayerInfo(PlayerType playerType)
		{
			if (playerType.Category != PlayerTypeCategory.Neutral) {
				throw new ArgumentException("Player type was of different category than neutral");
			}

			return new PlayerInfo(playerType, 0, PlayerInsignia.NeutralPlayerInsignia, false, true);
		}

		public static PlayerInfo CreateHumanPlayer(PlayerType playerType, int teamID, PlayerInsignia insignia)
		{
			if (playerType.Category != PlayerTypeCategory.Human) {
				throw new ArgumentException("Player type was not of type category human");
			}

			return new PlayerInfo(playerType, teamID, insignia, true, false);
		}

		public static PlayerInfo CreateAIPlayer(PlayerType playerType, int teamID, PlayerInsignia insignia)
		{
			if (playerType.Category != PlayerTypeCategory.AI) {
				throw new ArgumentException("Player type was not of type category AI");
			}

			return new PlayerInfo(playerType, teamID, insignia, false, false);
		}
	}

	public class PlayerSpecification : IEnumerable<PlayerInfo> {
		public static PlayerSpecification LoadFromSavedGame { get; } = new PlayerSpecification();

		public PlayerInfo NeutralPlayer { get; private set; }

		public PlayerInfo PlayerWithInput { get; private set; }

		readonly List<PlayerInfo> playerInfos;

		public PlayerSpecification()
		{
			playerInfos = new List<PlayerInfo>();
		}

		public void AddAIPlayer(PlayerType type, int teamID, PlayerInsignia insignia)
		{
			if (type == null) {
				throw new ArgumentNullException(nameof(type), "Cannot add AI player without AI");
			}
			playerInfos.Add(PlayerInfo.CreateAIPlayer(type, teamID, insignia));
		}

		public void SetNeutralPlayer(PlayerType type)
		{
			if (NeutralPlayer != null) {
				throw new InvalidOperationException("Level has to have exactly one neutral player");
			}

			if (type == null) {
				throw new ArgumentNullException(nameof(type), "Cannot add neutral AI player without AI");
			}

			NeutralPlayer = PlayerInfo.CreateNeutralPlayerInfo(type);
			playerInfos.Add(NeutralPlayer);
		}

		public void SetHumanPlayer(PlayerType type, int teamID, PlayerInsignia insignia)
		{
			if (PlayerWithInput != null) {
				throw new InvalidOperationException("Level has to have exactly one human player");
			}

			//Can add human player without AI

			PlayerWithInput = PlayerInfo.CreateHumanPlayer(type, teamID, insignia);
			playerInfos.Add(PlayerWithInput);
		}

		public IEnumerator<PlayerInfo> GetEnumerator()
		{
			if (NeutralPlayer == null) {
				throw new
					InvalidOperationException("Neutral player was not specified, level has to have exactly one neutral player");
			}

			if (PlayerWithInput == null) {
				throw new
					InvalidOperationException("Player with input was not specified, level has to have exactly one player with input");
			}

			return playerInfos.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
