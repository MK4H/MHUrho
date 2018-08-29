using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
	interface ILoader {

		void StartLoading();

		void ConnectReferences();

		void FinishLoading();
	}
}
