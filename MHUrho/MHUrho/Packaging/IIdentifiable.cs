using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Packaging
{
	/// <summary>
	/// Can be uniquely identified
	/// </summary>
	public interface IIdentifiable
	{
		/// <summary>
		/// Unique identifier local to the <see cref="GamePack"/> <see cref="Package"/>
		/// </summary>
		int ID { get; }
		
		/// <summary>
		/// Unique name local to the <see cref="GamePack"/> <see cref="Package"/>
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Source <see cref="GamePack"/> in which the <see cref="ID"/> and <see cref="Name"/> are unique
		/// </summary>
		GamePack Package { get; }
	}
}
