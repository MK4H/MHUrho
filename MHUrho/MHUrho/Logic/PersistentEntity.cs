using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
    public abstract class PersistentEntity : Entity
    {
		protected PersistentEntity(int ID, ILevelManager level)
			: base(ID, level)
		{ }
	}
}
