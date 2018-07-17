﻿using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using Urho;

namespace MHUrho.Input
{
	public interface IMenuController
	{
		InputType InputType { get; }

		bool Enabled { get; }

		void Enable();

		void Disable();

		void SwitchToPauseMenu(IGameController gameController);

		void ResumePausedLevel();

		void EndPausedLevel();
	}
}
