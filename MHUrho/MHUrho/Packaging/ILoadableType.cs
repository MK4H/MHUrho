﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Storage;

namespace MHUrho.Packaging
{
	public interface ILoadableType : IIDNameAndPackage {
		void Load(XElement xml, GamePack package);

		void ClearCache();
	}
}
