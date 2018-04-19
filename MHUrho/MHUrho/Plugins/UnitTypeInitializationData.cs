using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Plugins
{
	/// <summary>
	/// This class is used to give the user a way to define the unit type in code instead of XML
	/// 
	/// Every entry set to anything else other than null will override the data provided in XML
	/// </summary>
	public class UnitTypeInitializationData {
		public string ModelPath = null;
		public string MaterialPath = null;
		public string AnimationPath = null;

		/// <summary>
		/// Contains the full name (GamePack/Resource) of resource as key and amount as value
		/// </summary>
		public Dictionary<string, int> ResourcesNeeded = null;
	}
}
