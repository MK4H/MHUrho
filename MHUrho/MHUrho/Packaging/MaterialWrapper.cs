using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Urho;

namespace MHUrho.Packaging
{
    public abstract class MaterialWrapper : IDisposable {

		public static MaterialWrapper FromXml(XElement materialElement, GamePack package)
		{
			if (materialElement.Name != ModelXml.Inst.Material) {
				throw new
					ArgumentException($"Invalid element provided, expected {ModelXml.Inst.Material}, got {materialElement.Name}",
									nameof(materialElement));
			}

			IEnumerable<XElement> children = materialElement.Elements();

			XElement element;
			try {
				element = children.First();
			}
			catch (InvalidOperationException e) {
				throw new ArgumentException("Material element is not valid according to GamePack.xsd", nameof(materialElement), e);
			}

			if (element.Name == MaterialXml.Inst.MaterialListPath) {
				return new MaterialList(element.Value);
			}
			else if (element.Name == MaterialXml.Inst.SimpleMaterialPath) {
				return new SimpleMaterial(element.Value, package);
			}
			else if (element.Name == MaterialXml.Inst.GeometryMaterial) {
				return new GeometryMaterials(element, package);
			}
			else {
				throw new ArgumentException("Material element is not valid according to GamePack.xsd", nameof(materialElement));
			}
		}

		public abstract void ApplyMaterial(StaticModel model);
		public abstract void Dispose();
	}

	public class MaterialList : MaterialWrapper {
		readonly string materialListPath;

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

		readonly Material material;

		/// <summary>
		/// Loads simple material from the given path.
		/// </summary>
		/// <param name="materialPath">Path to the simple material.</param>
		/// <param name="package">Source package.</param>
		/// <exception cref="IOException"/>
		public SimpleMaterial(string materialPath, GamePack package) {
			this.material = package.PackageManager.GetMaterial(materialPath);
		}

		public override void ApplyMaterial(StaticModel model) {
			model.Material = material;
		}

		public override void Dispose() {
			material.Dispose();
		}
	}

	public class GeometryMaterials : MaterialWrapper {

		readonly List<Tuple<uint, Material>> materials;

		public GeometryMaterials(XElement element, GamePack package)
		{
			if (element.Name != MaterialXml.Inst.GeometryMaterial) {
				throw new
					ArgumentException($"Invalid element, expected {MaterialXml.Inst.GeometryMaterial}, got {element.Name}");
			}

			this.materials = new List<Tuple<uint, Material>>();

			foreach (XElement pathElement in element.Elements()) {
				XAttribute indexAttr = pathElement.Attribute(GeometryMaterialXml.Inst.IndexAttribute) ?? 
										throw new ArgumentException("GeometryMaterials element is not valid according to GamePack.xsd, missing index attribute");

				uint index = uint.TryParse(indexAttr.Value, out var value) ? 
								value : 
								throw new ArgumentException("GeometryMaterials element is not valid according to GamePack.xsd, invalid index value type");
				Material material;
				try {
					string path = FileManager.ReplaceDirectorySeparators(pathElement.Value);

					material = package.PackageManager.GetMaterial(path);
				}
				catch (Exception e) {
					throw new ArgumentException("Loading material failed, probably wrong path in the MaterialPath element", e);
				}

				materials.Add(Tuple.Create(index, material));

			}
		}

		public override void ApplyMaterial(StaticModel model)
		{
			foreach (var geometryMaterial in materials) {
				if (!model.SetMaterial(geometryMaterial.Item1, geometryMaterial.Item2)) {
					throw new
						InvalidOperationException($"Setting material on the geometry {geometryMaterial.Item1} failed");
				}
			}
		}

		public override void Dispose()
		{
			foreach (var geometryMat in materials) {
				geometryMat.Item2.Dispose();
			}
		}
	}
}
