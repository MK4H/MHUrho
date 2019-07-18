using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
	/// <summary>
	/// Is capable of three step loading.
	/// </summary>
	interface ILoader {

		/// <summary>
		/// Starts loading.
		/// </summary>
		void StartLoading();

		/// <summary>
		/// Connects the stored references of the loaded object to other loaded objects.
		/// </summary>
		void ConnectReferences();
		
		/// <summary>
		/// Cleans up.
		/// </summary>
		void FinishLoading();
	}
}
