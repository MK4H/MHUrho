using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using MHUrho.Plugins;

namespace MHUrho.Logic
{
	/// <summary>
	/// Represents type of entities loaded from package.
	/// </summary>
	public interface IEntityType : ILoadableType
	{
		/// <summary>
		/// The type plugin of this entity type.
		/// </summary>
		TypePlugin Plugin { get; }
	}
}
