using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.Packaging;
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

		void SavePausedLevel(string toPath);

		void StartLoadingLevel(LevelRep level, bool editorMode);

		void StartLoadingLevel(string savePath, bool editorMode);

		void StartLoadingDefaultLevel(IntVector2 mapSize);
	}
}
