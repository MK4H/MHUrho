using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Storage;

namespace MHUrho.Packaging
{
	/// <summary>
	/// Type that can be loaded from a package XML file
	/// </summary>
	public interface ILoadableType : IIdentifiable {

		/// <summary>
		/// Loads the type from the XML element <paramref name="xml"/>, which is taken from <paramref name="package"/> XML specification.
		/// </summary>
		/// <param name="xml">The XML element of the type.</param>
		/// <param name="package">Source package of the XML element</param>
		void Load(XElement xml, GamePack package);

		/// <summary>
		/// Clears any cache of instances the type might have held.
		/// Provided to enable resource pooling.
		/// </summary>
		void ClearCache();
	}
}
