using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Packaging
{
    public class ModelWrapper : IDisposable {
		private readonly Model model;

		private readonly Vector3 scale;


		public ModelWrapper(Model model) 
			:this(model, new Vector3(1,1,1))
		{

		}

		public ModelWrapper(Model model, Vector3 scale) {
			this.model = model;
			this.scale = scale;
		}

		public AnimatedModel AddModel(Node node) {
			node.Scale = scale;
			var animatedModel = node.CreateComponent<AnimatedModel>();
			animatedModel.Model = model;
			return animatedModel;
		}

		public void Dispose() {
			model.Dispose();
		}
	}
}
