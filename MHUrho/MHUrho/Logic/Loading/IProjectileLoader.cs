using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Logic
{
    interface IProjectileLoader : ILoader
    {
		IProjectile Projectile { get; }
    }
}
