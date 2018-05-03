using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Packaging;

namespace MHUrho.Plugins
{
	public abstract class TypePlugin
	{
		public abstract bool IsMyType(string typeName);

		/// <summary>
		/// Called to initialize the instance
		/// </summary>
		/// <param name="extensionElement">extension element of the unitType xml description or null if there is none</param>
		/// <param name="packageManager">package manager for connecting to other entityTypes</param>
		public abstract void Initialize(XElement extensionElement, PackageManager packageManager);
	}
}
