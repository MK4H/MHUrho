using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
	/// <summary>
	/// Implementation of the Visitor design patter.
	/// </summary>
    public interface IEntityVisitor {

		void Visit(IUnit unit);

		void Visit(IBuilding building);

		void Visit(IProjectile projectile);
	}

	/// <summary>
	/// Implementation of the generic Visitor design patter.
	/// </summary>
	public interface IEntityVisitor<out T> {

		T Visit(IUnit unit);

		T Visit(IBuilding building);

		T Visit(IProjectile projectile);
	}
}
