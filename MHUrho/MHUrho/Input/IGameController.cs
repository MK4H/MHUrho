using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using Urho;
using MHUrho.UserInterface;

namespace MHUrho.Input
{ 
	public interface IGameController : IDisposable
	{
		GameUIManager UIManager { get; }

		IPlayer Player { get; set; }

		bool Enabled { get; }

		bool DoOnlySingleRaycasts { get; set; }

		void Enable();

		void Disable();

	}
}
