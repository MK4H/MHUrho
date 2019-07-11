﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using Urho;
using MHUrho.UserInterface;

namespace MHUrho.Input
{ 
	public enum InputType { MouseAndKeyboard, Touch }

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
