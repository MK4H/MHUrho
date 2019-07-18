using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Physics;

namespace MHUrho.Logic
{
	/// <summary>
	/// Sets up components on the node hierarchy making up the unit to the
	/// correct values for units.
	/// </summary>
	class UnitComponentSetup : ComponentSetup
	{
		/// <inheritdoc />
		protected override void SetupRigidBody(RigidBody rigidBody, ILevelManager level)
		{ 
			rigidBody.CollisionLayer = (int)CollisionLayer.Unit;
			rigidBody.CollisionMask = (int)CollisionLayer.Projectile;
			rigidBody.Kinematic = true;
			rigidBody.Mass = 1;
			rigidBody.UseGravity = false;
		}

		/// <inheritdoc />
		protected override void SetupStaticModel(StaticModel staticModel, ILevelManager level)
		{
			staticModel.CastShadows = false;
			staticModel.DrawDistance = level.App.Config.UnitDrawDistance;
		}

		/// <inheritdoc />
		protected override void SetupAnimatedModel(AnimatedModel animatedModel, ILevelManager level)
		{
			SetupStaticModel(animatedModel, level);
		}

		/// <inheritdoc />
		protected override void SetupAnimationController(AnimationController animation, ILevelManager level)
		{

		}
	}
}
