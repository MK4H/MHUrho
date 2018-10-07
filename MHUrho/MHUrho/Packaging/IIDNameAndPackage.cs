using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Packaging
{
	public interface IIDNameAndPackage
	{
		int ID { get; }
		string Name { get; }
		GamePack Package { get; }
	}
}
