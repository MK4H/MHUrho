using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using MHUrho.Plugins;

namespace MHUrho.Logic
{
	public interface IEntityType : ILoadableType
	{
		TypePlugin Plugin { get; }
	}
}
