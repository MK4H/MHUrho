using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
	public class PlayerInfo {
		public bool IsNeutral { get; private set; }

		public bool HasInput { get; private set; }

		public PlayerType PlayerType { get; private set; }

		public int TeamID { get; private set; }

		public PlayerInfo(PlayerType playerType, int teamID, bool hasInput, bool isNeutral)
		{
			this.PlayerType = playerType;
			this.TeamID = teamID;
			this.HasInput = hasInput;
			this.IsNeutral = isNeutral;
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

		public void AddAIPlayer(PlayerType type, int teamID)
		{
			if (type == null) {
				throw new ArgumentNullException(nameof(type), "Cannot add AI player without AI");
			}
			playerInfos.Add(new PlayerInfo(type, teamID, false, false));
		}

		public void SetNeutralPlayer(PlayerType type)
		{
			if (NeutralPlayer != null) {
				throw new InvalidOperationException("Level has to have exactly one neutral player");
			}

			if (type == null) {
				throw new ArgumentNullException(nameof(type), "Cannot add neutral AI player without AI");
			}

			NeutralPlayer = new PlayerInfo(type, 0, false, true);
			playerInfos.Add(NeutralPlayer);
		}

		public void SetPlayerWithInput(PlayerType type, int teamID)
		{
			if (PlayerWithInput != null) {
				throw new InvalidOperationException("Level has to have exactly one player with input");
			}

			//Can add human player without AI

			PlayerWithInput = new PlayerInfo(type, teamID, true, false);
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
