using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using Urho;
using MHUrho.UserInterface;

namespace MHUrho.Input
{ 
	/// <summary>
	/// Represents an input schema type.
	/// </summary>
	public enum InputType { MouseAndKeyboard, Touch }

	/// <summary>
	/// Provides access to the user input during the game.
	/// Controls the level lifetime.
	/// </summary>
	public interface IGameController : IDisposable
	{
		GameUIManager UIManager { get; }

		ILevelManager Level { get; }

		IPlayer Player { get; }

		bool Enabled { get; }

		InputType InputType { get; }

		bool DoOnlySingleRaycasts { get; set; }

		void Enable();

		void Disable();

		void Pause();

		void UnPause();

		void EndLevelToEndScreen(bool victory);

		void EndLevel();

		List<RayQueryResult> CursorRaycast();

		ITile GetTileUnderCursor();

		void ChangeControllingPlayer(IPlayer newControllingPlayer);
	}
}
