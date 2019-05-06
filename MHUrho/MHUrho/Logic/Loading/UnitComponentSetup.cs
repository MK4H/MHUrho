﻿using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Physics;

namespace MHUrho.Logic
{
	class UnitComponentSetup : ComponentSetup
	{
		protected override void SetupRigidBody(RigidBody rigidBody, ILevelManager level)
		{ 
			rigidBody.CollisionLayer = (int)CollisionLayer.Unit;
			rigidBody.CollisionMask = (int)CollisionLayer.Projectile;
			rigidBody.Kinematic = true;
			rigidBody.Mass = 1;
			rigidBody.UseGravity = false;
		}

		protected override void SetupStaticModel(StaticModel staticModel, ILevelManager level)
		{
			staticModel.CastShadows = false;
			staticModel.DrawDistance = level.App.Config.UnitDrawDistance;
		}

		protected override void SetupAnimatedModel(AnimatedModel animatedModel, ILevelManager level)
		{
			SetupStaticModel(animatedModel, level);
		}

		protected override void SetupAnimationController(AnimationController animation, ILevelManager level)
		{

		}
	}
}