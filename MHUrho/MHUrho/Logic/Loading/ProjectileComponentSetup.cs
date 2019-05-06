using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Physics;

namespace MHUrho.Logic
{
	class ProjectileComponentSetup : ComponentSetup
	{
		protected override void SetupRigidBody(RigidBody rigidBody, ILevelManager level)
		{
			rigidBody.CollisionLayer = (int)CollisionLayer.Projectile;
			rigidBody.CollisionMask = (int)(CollisionLayer.Unit | CollisionLayer.Building);
			rigidBody.Kinematic = true;
			rigidBody.Mass = 1;
			rigidBody.UseGravity = false;
		}

		protected override void SetupStaticModel(StaticModel staticModel, ILevelManager level)
		{
			staticModel.CastShadows = false;
			staticModel.DrawDistance = level.App.Config.ProjectileDrawDistance;
		}

		protected override void SetupAnimatedModel(AnimatedModel animatedModel, ILevelManager level)
		{
			SetupStaticModel(animatedModel, level);
		}

		protected override void SetupAnimationController(AnimationController animationController, ILevelManager level)
		{
			
		}
	}
}
