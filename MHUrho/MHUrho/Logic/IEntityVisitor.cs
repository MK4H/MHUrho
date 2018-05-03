using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
    public interface IEntityVisitor {

		void Visit(IUnit unit);

		void Visit(IBuilding building);

		void Visit(IProjectile projectile);
	}
}
