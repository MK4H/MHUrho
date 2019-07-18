using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Physics;

namespace MHUrho.Logic
{

	/// <summary>
	/// Base class for component setup classes that traverse the node hierarchy
	/// making up entities and set the properties of components on these nodes to
	/// needed values.
	/// </summary>
	abstract class ComponentSetup
	{
		/// <summary>
		/// Encapsulates methods for setting a component properties.
		/// </summary>
		/// <param name="component">The component to set properties on.</param>
		/// <param name="level">The level manager.</param>
		delegate void ComponentSetupDelegate(Component component, ILevelManager level);

		/// <summary>
		/// Mapping of component types to the methods that set them up.
		/// </summary>
		readonly Dictionary<StringHash, ComponentSetupDelegate> setupDispatch;

		/// <summary>
		/// Sets upa the dispatching.
		/// </summary>
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


		/// <summary>
		/// Traverses the node hierarchy by DFS search starting from <paramref name="node"/>.
		/// </summary>
		/// <param name="node">The node to start the search on.</param>
		/// <param name="level">Level manager.</param>
		public virtual void SetupComponentsOnNode(Node node, ILevelManager level)
		{
			Stack<Node> nodesToSetup = new Stack<Node>();
			nodesToSetup.Push(node);

			while (nodesToSetup.Count != 0) {
				Node current = nodesToSetup.Pop();

				SetupNodeComponents(current, level);
				foreach (var child in current.Children) {
					nodesToSetup.Push(child);
				}
			}
		}

		/// <summary>
		/// Setup the <see cref="RigidBody"/> component.
		/// </summary>
		/// <param name="rigidBody">The rigid body component to setup.</param>
		/// <param name="level">Level manager.</param>
		protected abstract void SetupRigidBody(RigidBody rigidBody, ILevelManager level);

		/// <summary>
		/// Setup the <see cref="StaticModel"/> component.
		/// </summary>
		/// <param name="staticModel">The static model component to setup.</param>
		/// <param name="level">Level manager.</param>
		protected abstract void SetupStaticModel(StaticModel staticModel, ILevelManager level);

		/// <summary>
		/// Setup the <see cref="AnimatedModel"/> component.
		/// </summary>
		/// <param name="animatedModel">The animated model component to setup.</param>
		/// <param name="level">Level manager.</param>
		protected abstract void SetupAnimatedModel(AnimatedModel animatedModel, ILevelManager level);

		/// <summary>
		/// Setup the <see cref="AnimationController"/> component.
		/// </summary>
		/// <param name="animationController">The animation controller component to setup.</param>
		/// <param name="level">Level manager.</param>
		protected abstract void SetupAnimationController(AnimationController animationController, ILevelManager level);

		/// <summary>
		/// Transforms the weakly typed dispatch to strongly typed method call for rigid body.
		/// </summary>
		/// <param name="rigidBodyComponent">The rigid body component.</param>
		/// <param name="level">Level manager.</param>
		void SetupRigidBodyWeak(Component rigidBodyComponent, ILevelManager level)
		{
			SetupRigidBody((RigidBody) rigidBodyComponent, level);
		}

		/// <summary>
		/// Transforms the weakly typed dispatch to strongly typed method call for static model.
		/// </summary>
		/// <param name="staticModelComponent">The static model component.</param>
		/// <param name="level">Level manager.</param>
		void SetupStaticModelWeak(Component staticModelComponent, ILevelManager level)
		{
			SetupStaticModel((StaticModel) staticModelComponent, level);
		}

		/// <summary>
		/// Transforms the weakly typed dispatch to strongly typed method call for animated model.
		/// </summary>
		/// <param name="animatedModelComponent">The animated model component.</param>
		/// <param name="level">Level manager.</param>
		void SetupAnimatedModelWeak(Component animatedModelComponent, ILevelManager level)
		{
			SetupAnimatedModel((AnimatedModel)animatedModelComponent, level);
		}

		/// <summary>
		/// Transforms the weakly typed dispatch to strongly typed method call for animation controller.
		/// </summary>
		/// <param name="animationControllerComponent">The animation controller component.</param>
		/// <param name="level">Level manager.</param>
		void SetupAnimationControllerWeak(Component animationControllerComponent, ILevelManager level)
		{
			SetupAnimationController((AnimationController)animationControllerComponent, level);
		}

		/// <summary>
		/// Sets up the components on the given <paramref name="node"/>.
		/// </summary>
		/// <param name="node">The node to setup the components on.</param>
		/// <param name="level">Level manager.</param>
		void SetupNodeComponents(Node node, ILevelManager level)
		{
			foreach (var component in node.Components)
			{
				if (setupDispatch.TryGetValue(component.Type, out ComponentSetupDelegate value))
				{
					value(component, level);
				}
			}
		}
	}
}
