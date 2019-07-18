using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MHUrho.EntityInfo;

namespace MHUrho.Logic
{

	/// <summary>
	/// Data for player loading.
	/// </summary>
	public class PlayerInfo {
		/// <summary>
		/// If the player will be neutral player.
		/// Mutually exclusive with <see cref="IsHuman"/>.
		/// </summary>
		public bool IsNeutral { get; private set; }

		/// <summary>
		/// If the player will be human player.
		/// Mutually exclusive with <see cref="IsNeutral"/>.
		/// </summary>
		public bool IsHuman { get; private set; }

		/// <summary>
		/// Type of the player.
		/// </summary>
		public PlayerType PlayerType { get; private set; }

		/// <summary>
		/// The players team.
		/// </summary>
		public int TeamID { get; private set; }

		/// <summary>
		/// Graphical identifications of the player.
		/// </summary>
		public PlayerInsignia Insignia { get; private set; }

		protected PlayerInfo(PlayerType playerType, int teamID, PlayerInsignia insignia, bool isHuman, bool isNeutral)
		{
			this.PlayerType = playerType;
			this.TeamID = teamID;
			this.Insignia = insignia;
			this.IsHuman = isHuman;
			this.IsNeutral = isNeutral;
		}

		/// <summary>
		/// Creates player info for neutral player that will be of the given <paramref name="playerType"/>.
		/// Checks that the given player type has category <see cref="PlayerTypeCategory.Neutral"/>
		/// </summary>
		/// <param name="playerType">The type of the player.</param>
		/// <returns>Info to initialize the player with.</returns>
		/// <exception cref="ArgumentException">Thrown when the given <paramref name="playerType"/> is not neutral category.</exception>
		public static PlayerInfo CreateNeutralPlayerInfo(PlayerType playerType)
		{
			if (playerType.Category != PlayerTypeCategory.Neutral) {
				throw new ArgumentException("Player type was of different category than neutral");
			}

			return new PlayerInfo(playerType, 0, PlayerInsignia.NeutralPlayerInsignia, false, true);
		}

		/// <summary>
		/// Creates player info for human player that will be of the given <paramref name="playerType"/>.
		/// Checks that the given player type has category <see cref="PlayerTypeCategory.Human"/>
		/// </summary>
		/// <param name="playerType">The type of the player.</param>
		/// <param name="teamID">ID of the team the human player will be part of.</param>
		/// <param name="insignia">The graphical identifications of the player.</param>
		/// <returns>Info to initialize the player with.</returns>
		/// <exception cref="ArgumentException">Thrown when the given <paramref name="playerType"/> is not human category.</exception>
		public static PlayerInfo CreateHumanPlayer(PlayerType playerType, int teamID, PlayerInsignia insignia)
		{
			if (playerType.Category != PlayerTypeCategory.Human) {
				throw new ArgumentException("Player type was not of type category human");
			}

			return new PlayerInfo(playerType, teamID, insignia, true, false);
		}

		/// <summary>
		/// Creates player info for ai player that will be of the given <paramref name="playerType"/>.
		/// Checks that the given player type has category <see cref="PlayerTypeCategory.AI"/>
		/// </summary>
		/// <param name="playerType">The type of the player.</param>
		/// <param name="teamID">ID of the team the ai player will be part of.</param>
		/// <param name="insignia">The graphical identifications of the player.</param>
		/// <returns>Info to initialize the player with.</returns>
		/// <exception cref="ArgumentException">Thrown when the given <paramref name="playerType"/> is not ai category.</exception>
		public static PlayerInfo CreateAIPlayer(PlayerType playerType, int teamID, PlayerInsignia insignia)
		{
			if (playerType.Category != PlayerTypeCategory.AI) {
				throw new ArgumentException("Player type was not of type category AI");
			}

			return new PlayerInfo(playerType, teamID, insignia, false, false);
		}
	}

	/// <summary>
	/// Specification of all players for level.
	/// </summary>
	public class PlayerSpecification : IEnumerable<PlayerInfo> {
		/// <summary>
		/// Specification for levels that are loaded from saved game.
		/// </summary>
		public static PlayerSpecification LoadFromSavedGame { get; } = new PlayerSpecification();

		/// <summary>
		/// Settings of the neutral player.
		/// </summary>
		public PlayerInfo NeutralPlayer { get; private set; }
		
		/// <summary>
		/// Settings of the human player.
		/// </summary>
		public PlayerInfo PlayerWithInput { get; private set; }

		/// <summary>
		/// Settings of the ai players.
		/// </summary>
		readonly List<PlayerInfo> playerInfos;

		public PlayerSpecification()
		{
			playerInfos = new List<PlayerInfo>();
		}

		/// <summary>
		/// Adds AI player to the specification of players.
		/// </summary>
		/// <param name="type">The type of the player.</param>
		/// <param name="teamID">The team the player will be part of.</param>
		/// <param name="insignia">The graphical representation of the player.</param>
		public void AddAIPlayer(PlayerType type, int teamID, PlayerInsignia insignia)
		{
			if (type == null) {
				throw new ArgumentNullException(nameof(type), "Cannot add AI player without AI");
			}
			playerInfos.Add(PlayerInfo.CreateAIPlayer(type, teamID, insignia));
		}

		/// <summary>
		/// Sets the settings of the neutral player.
		/// </summary>
		/// <param name="type">The neutral player type.</param>
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

		/// <summary>
		/// Sets the human player settings.
		/// </summary>
		/// <param name="type">The type of the player.</param>
		/// <param name="teamID">The team the player will be part of.</param>
		/// <param name="insignia">The graphical representation of the player.</param>
		public void SetHumanPlayer(PlayerType type, int teamID, PlayerInsignia insignia)
		{
			if (PlayerWithInput != null) {
				throw new InvalidOperationException("Level has to have exactly one human player");
			}

			//Can add human player without AI

			PlayerWithInput = PlayerInfo.CreateHumanPlayer(type, teamID, insignia);
			playerInfos.Add(PlayerWithInput);
		}

		/// <summary>
		/// Returns an enumerator that enumerates the AI players.
		/// First checks that neutral player and human player were both set.
		/// </summary>
		/// <returns>An enumerator that enumerates the settings of AI players.</returns>
		/// <exception cref="InvalidOperationException">Thrown when human or neutral player were not set at the time of the call of this method.</exception>
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

		/// <summary>
		/// Returns an enumerator that enumerates the AI players.
		/// First checks that neutral player and human player were both set.
		/// </summary>
		/// <returns>An enumerator that enumerates the settings of AI players.</returns>
		/// <exception cref="InvalidOperationException">Thrown when human or neutral player were not set at the time of the call of this method.</exception>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
