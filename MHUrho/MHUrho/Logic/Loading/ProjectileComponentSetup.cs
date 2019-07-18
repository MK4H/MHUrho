using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Physics;

namespace MHUrho.Logic
{
	/// <summary>
	/// Sets up components on the node hierarchy making up the projectile
	/// to correct values for projectile.
	/// </summary>
	class ProjectileComponentSetup : ComponentSetup
	{
		/// <inheritdoc />
		protected override void SetupRigidBody(RigidBody rigidBody, ILevelManager level)
		{
			rigidBody.CollisionLayer = (int)CollisionLayer.Projectile;
			rigidBody.CollisionMask = (int)(CollisionLayer.Unit | CollisionLayer.Building);
			rigidBody.Kinematic = true;
			rigidBody.Mass = 1;
			rigidBody.UseGravity = false;
		}

		/// <inheritdoc />
		protected override void SetupStaticModel(StaticModel staticModel, ILevelManager level)
		{
			staticModel.CastShadows = false;
			staticModel.DrawDistance = level.App.Config.ProjectileDrawDistance;
		}

		/// <inheritdoc />
		protected override void SetupAnimatedModel(AnimatedModel animatedModel, ILevelManager level)
		{
			SetupStaticModel(animatedModel, level);
		}

		/// <inheritdoc />
		protected override void SetupAnimationController(AnimationController animationController, ILevelManager level)
		{
			
		}
	}
}
