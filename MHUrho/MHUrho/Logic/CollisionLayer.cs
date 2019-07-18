using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{

	/// <summary>
	/// Which entities will the collider collide with.
	/// </summary>
	[Flags]
	enum CollisionLayer {
		Unit = 1,
		Building = 2,
		Projectile = 4
	}
	
}
