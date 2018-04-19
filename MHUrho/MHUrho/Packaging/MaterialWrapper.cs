using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Packaging
{
    public abstract class MaterialWrapper : IDisposable {
		public abstract void ApplyMaterial(StaticModel model);
		public abstract void Dispose();
	}

	public class MaterialList : MaterialWrapper {
		private readonly string materialListPath;

		public MaterialList(string materialListPath) {
			this.materialListPath = materialListPath;
		}

		public override void ApplyMaterial(StaticModel model) {
			model.ApplyMaterialList(materialListPath);
		}

		public override void Dispose() {

		}
	}

	public class SimpleMaterial : MaterialWrapper {

		private readonly Material material;

		public SimpleMaterial(Material material) {
			this.material = material;
		}

		public override void ApplyMaterial(StaticModel model) {
			model.Material = material;
		}

		public override void Dispose() {
			material.Dispose();
		}

	}
}
