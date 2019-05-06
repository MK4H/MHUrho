using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Physics;

namespace MHUrho.Logic
{

	abstract class ComponentSetup
	{
		delegate void ComponentSetupDelegate(Component component, ILevelManager level);

		readonly Dictionary<StringHash, ComponentSetupDelegate> setupDispatch;

		protected ComponentSetup()
		{
			setupDispatch = new Dictionary<StringHash, ComponentSetupDelegate>
							{
								{ RigidBody.TypeStatic, SetupRigidBodyWeak },
								{ StaticModel.TypeStatic, SetupStaticModelWeak },
								{ AnimatedModel.TypeStatic, SetupAnimatedModelWeak },
								{ AnimationController.TypeStatic, SetupAnimationControllerWeak }
							};
		}


		public virtual void SetupComponentsOnNode(Node node, ILevelManager level)
		{
			//TODO: Maybe loop through child nodes
			foreach (var component in node.Components)
			{
				if (setupDispatch.TryGetValue(component.Type, out ComponentSetupDelegate value))
				{
					value(component, level);
				}
			}
		}

		protected abstract void SetupRigidBody(RigidBody rigidBody, ILevelManager level);

		protected abstract void SetupStaticModel(StaticModel staticModel, ILevelManager level);

		protected abstract void SetupAnimatedModel(AnimatedModel animatedModel, ILevelManager level);

		protected abstract void SetupAnimationController(AnimationController animationController, ILevelManager level);

		void SetupRigidBodyWeak(Component rigidBodyComponent, ILevelManager level)
		{
			SetupRigidBody((RigidBody) rigidBodyComponent, level);
		}

		void SetupStaticModelWeak(Component staticModelComponent, ILevelManager level)
		{
			SetupStaticModel((StaticModel) staticModelComponent, level);
		}

		void SetupAnimatedModelWeak(Component animatedModelComponent, ILevelManager level)
		{
			SetupAnimatedModel((AnimatedModel)animatedModelComponent, level);
		}

		void SetupAnimationControllerWeak(Component animationControllerComponent, ILevelManager level)
		{
			SetupAnimationController((AnimationController)animationControllerComponent, level);
		}
	}
}
