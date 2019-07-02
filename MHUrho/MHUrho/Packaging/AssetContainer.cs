using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho;
using Urho.Physics;
using Urho.Resources;

namespace MHUrho.Packaging
{
	public abstract class AssetContainer : IDisposable {

		public abstract Node Instantiate(ILevelManager level, Vector3 position, Quaternion rotation);

		public abstract void Dispose();

		/// <summary>
		/// Creates a container for assets based on the description in he <paramref name="assetsElement"/>.
		/// </summary>
		/// <param name="assetsElement">XML element describing the assets to load.</param>
		/// <param name="package">Package to load the assets from.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="assetsElement"/> is null or there is some internal error</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="assetsElement"/> does not conform to the xml schema or there is some internal error</exception>
		/// <exception cref="ResourceLoadingException">Thrown when the resource described by the value of <paramref name="assetsElement"/> could not be loaded</exception>
		public static AssetContainer FromXml(XElement assetsElement, GamePack package)
		{
			Check(assetsElement);

			string type = assetsElement.Attribute(AssetsXml.Inst.TypeAttribute).Value;

			switch (type) {
				case AssetsXml.XmlPrefabType:
					return new XmlPrefabAssetContainer(assetsElement, package);
				case AssetsXml.BinaryPrefabType:
					return new BinaryPrefabAssetContainer(assetsElement, package);
				case AssetsXml.ItemsType:
					return new ItemsAssetContainer(assetsElement, package);
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown asset type");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="assetsElement"></param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="assetsElement"/> is null</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="assetsElement"/> xml element is not the <see cref="EntityXml.Inst.Assets"/> element</exception>
		protected static void Check(XElement assetsElement)
		{
			if (assetsElement == null)
			{
				throw new ArgumentNullException(nameof(assetsElement), "Assets element cannot be null");
			}

			if (assetsElement.Name != EntityXml.Inst.Assets)
			{
				throw new ArgumentException($"Can only load from {EntityXml.Inst.Assets}", nameof(assetsElement));
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="assetsElement"></param>
		/// <param name="wantedType"></param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="assetsElement"/> is null</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="assetsElement"/> xml element is not the <see cref="EntityXml.Inst.Assets"/> element
		/// or the type attribute is not <paramref name="wantedType"/></exception>
		protected static void CheckWithType(XElement assetsElement, string wantedType)
		{
			Check(assetsElement);

			string type = assetsElement.Attribute(AssetsXml.Inst.TypeAttribute)?.Value;
			if (type == null || type != wantedType)
			{
				throw new ArgumentException("Unexpected assets element type, expected BinaryPrefab", nameof(
												assetsElement));
			}
		}
	}

	public abstract class FilePrefabAssetContainer : AssetContainer {

		/// <summary>
		/// 
		/// </summary>
		/// <param name="assetsElement"></param>

		/// <exception cref="IOException">When the file specified by the path in the <see cref="AssetsElementXml.Inst.Path"/> cannot be opened</exception>
		protected string GetPath(XElement assetsElement)
		{
			var pathElement = assetsElement.Element(AssetsXml.Inst.Path);
			return FileManager.ReplaceDirectorySeparators(pathElement.Value);
		}
	}

	public class XmlPrefabAssetContainer : FilePrefabAssetContainer {

		readonly XmlFile file;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="assetsElement"></param>
		/// <exception cref="ResourceNotFoundException">Thrown when the xml prefab file could not be found</exception>
		public XmlPrefabAssetContainer(XElement assetsElement, GamePack package)
		{
			CheckWithType(assetsElement, AssetsXml.XmlPrefabType);

			string relativePath = GetPath(assetsElement);
			this.file = package.PackageManager.GetXmlFile(relativePath, true);
		}

		public override Node Instantiate(ILevelManager level, Vector3 position, Quaternion rotation)
		{
			Node newNode = null;
			try {
				newNode = level.Scene.InstantiateXml(file.GetRoot(), position, rotation);
			}
			catch (Exception e) {
				string message = $"Prefab instantiation failed with an exception: {e.Message}";
				Urho.IO.Log.Write(LogLevel.Warning,
								message);
				throw new LevelLoadingException($"Prefab instantiation failed with an exception: {e.Message}");
			}
			newNode.ChangeParent(level.LevelNode);

			return newNode;
		}

		public override void Dispose()
		{
			file.Dispose();
		}
	}

	public class BinaryPrefabAssetContainer : FilePrefabAssetContainer {
		readonly Urho.IO.File file;

		/// <summary>
		/// Loads a binary prefab based on the <paramref name="assetsElement"/>.
		/// </summary>
		/// <param name="assetsElement">XML containing the path to the binary prefab.</param>
		/// <param name="package">GamePack of the level.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="assetsElement"/> is null</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="assetsElement"/> xml element is not valid, either is not the <see cref="EntityXml.Inst.Assets"/> element
		/// or has a wrong type specified</exception>
		/// <exception cref="IOException">When the file specified by the path in the <see cref="AssetsElementXml.Inst.Path"/> cannot be opened</exception>
		/// <exception cref="ResourceLoadingException"> Thrown when the file could not be found </exception>
		public BinaryPrefabAssetContainer(XElement assetsElement, GamePack package)
		{
			CheckWithType(assetsElement, AssetsXml.BinaryPrefabType);
			var relativePath = GetPath(assetsElement);

			this.file = package.PackageManager.GetFile(relativePath, true);
		}

		public override Node Instantiate(ILevelManager level, Vector3 position, Quaternion rotation)
		{
			var newNode = level.Scene.Instantiate(file, position, rotation);
			newNode.ChangeParent(level.LevelNode);

			return newNode;
		}

		public override void Dispose()
		{
			file.Dispose();
		}
	}

	public class ItemsAssetContainer : AssetContainer {

		delegate AssetLoader ParseAssetLoaderDelegate(XElement element, GamePack package);

		abstract class AssetLoader : IDisposable {
			public abstract void ApplyOn(Node node);

			public virtual void Dispose()
			{

			}
		}

		class StaticModelLoader : AssetLoader {
			protected readonly Model Model;

			protected readonly MaterialWrapper Material;

			protected StaticModelLoader(Model model, MaterialWrapper material)
			{
				this.Model = model;
				this.Material = material;
			}

			/// <summary>
			/// Provides derived classes with the ability to construct an instance of StaticModelLoader
			/// </summary>
			/// <param name="model"></param>
			/// <param name="material"></param>
			/// <returns></returns>
			protected static StaticModelLoader CreateStaticModel(Model model, MaterialWrapper material)
			{
				return new StaticModelLoader(model, material);
			}

			public override void ApplyOn(Node node)
			{
				var staticModel = CreateComponent(node);
				staticModel.Model = Model;
				Material.ApplyMaterial(staticModel);
			}

			public virtual StaticModel CreateComponent(Node node)
			{
				return node.CreateComponent<StaticModel>();
			}

			public override void Dispose()
			{
				Model.Dispose();
			}
		}

		class AnimatedModelLoader : StaticModelLoader {

			/// <summary>
			/// Loads animated model based on the descriptio in the XML <paramref name="element"/>.
			/// </summary>
			/// <param name="element">XML describing the animated model.</param>
			/// <param name="package">GamePackage of the level.</param>
			/// <returns>Loader with the loaded model.</returns>
			/// <exception cref="ArgumentNullException">When the <paramref name="element"/> is null</exception>
			/// <exception cref="ArgumentException">Thrown when the <paramref name="element"/> does not conform to the xml schema</exception>
			/// <exception cref="ResourceLoadingException">Thrown when either the model or material resource could not be loaded</exception>
			public static StaticModelLoader Load(XElement element, GamePack package)
			{
				if (element == null)
				{
					throw new ArgumentNullException(nameof(element), "Element was null");
				}

				if (element.Name != AssetsXml.Inst.Model)
				{
					throw new ArgumentException("Invalid element", nameof(element));
				}

				string type = element.Attribute(ModelXml.Inst.TypeAttribute).Value;

				string modelPath = element.Element(ModelXml.Inst.ModelPath).Value;


				Model model = package.PackageManager.GetModel(modelPath, true);

				XElement materialElement  = element.Element(ModelXml.Inst.Material);

				MaterialWrapper material = MaterialWrapper.FromXml(materialElement, package);

				if (type == ModelXml.StaticModelType) {
					return StaticModelLoader.CreateStaticModel(model, material);
				}
				else if (type == ModelXml.AnimatedModelType) {
					return new AnimatedModelLoader(model, material);
				}
				else {
					throw new ArgumentException("Model xml contained unknown type", nameof(element));
				}
			}

			protected AnimatedModelLoader(Model model, MaterialWrapper material)
				:base(model, material)
			{

			}

			public override StaticModel CreateComponent(Node node)
			{
				return node.CreateComponent<AnimatedModel>();
			}
		}

		class ScaleLoader : AssetLoader {
			readonly Vector3 scale;

			/// <summary>
			/// Loads a scale from given XML.
			/// </summary>
			/// <param name="element">XML describing the scale.</param>
			/// <param name="package">Source package of the XML.</param>
			/// <returns>Loaded scale.</returns>
			/// <exception cref="ArgumentNullException">Thrown when the <paramref name="element"/> is null</exception>
			/// <exception cref="ArgumentException">Thrown when the <paramref name="element"/> does not conform to the xml schema</exception>
			public static ScaleLoader Load(XElement element, GamePack package)
			{
				if (element == null)
				{
					throw new ArgumentNullException(nameof(element), "Element was null");
				}

				if (element.Name != AssetsXml.Inst.Scale)
				{
					throw new ArgumentException("Invalid element", nameof(element));
				}

				Vector3 scale = XmlHelpers.GetVector3(element);

				return new ScaleLoader(scale);
			}

			protected ScaleLoader(Vector3 scale)
			{
				this.scale = scale;
			}

			public override void ApplyOn(Node node)
			{
				node.Scale = scale;
			}
		}

		class CollisionShapeLoader : AssetLoader {
			delegate ConcreteShape LoadShapeDelegate(XElement element, GamePack package);

			abstract class ConcreteShape {
				public abstract void SetTo(CollisionShape shapeComponent);

				protected static Vector3 GetPosition(XElement element)
				{
					return element.Element(CollisionShapeXml.Inst.Position)?.GetVector3() ?? new Vector3(0, 0, 0);
				}

				protected static Quaternion GetRotation(XElement element)
				{
					return element.Element(CollisionShapeXml.Inst.Rotation)?.GetQuaternion() ?? new Quaternion(0, 0, 0);
				}
			}

			class Box : ConcreteShape {

				public static XName ElementName => CollisionShapeXml.Inst.Box;

				readonly Vector3 size;
				readonly Vector3 position;
				readonly Quaternion rotation;

				Box(Vector3 size, Vector3 position, Quaternion rotation)
				{
					this.size = size;
					this.position = position;
					this.rotation = rotation;
				}

				public static Box FromXml(XElement boxElement, GamePack package)
				{
					if (boxElement.Name != ElementName)
					{
						throw new ArgumentException($"Expected element {ElementName}, got {boxElement.Name}", nameof(boxElement));
					}

					Vector3 size = boxElement.Element(CollisionShapeXml.Inst.Size).GetVector3();
					Vector3 position = GetPosition(boxElement);
					Quaternion rotation = GetRotation(boxElement);

					return new Box(size, position, rotation);
				}

				public override void SetTo(CollisionShape shapeComponent)
				{
					shapeComponent.SetBox(size, position, rotation);				}
			}

			class Capsule : ConcreteShape {

				public static XName ElementName => CollisionShapeXml.Inst.Capsule;

				readonly float diameter;
				readonly float height;
				readonly Vector3 position;
				readonly Quaternion rotation;


				Capsule(float diameter, float height, Vector3 position, Quaternion rotation)
				{
					this.diameter = diameter;
					this.height = height;
					this.position = position;
					this.rotation = rotation;
				}

				public static Capsule FromXml(XElement capsuleElement, GamePack package)
				{
					if (capsuleElement.Name != ElementName)
					{
						throw new ArgumentException($"Expected element {ElementName}, got {capsuleElement.Name}", nameof(capsuleElement));
					}

					float diameter = capsuleElement.Element(CollisionShapeXml.Inst.Diameter).GetFloat();
					float height = capsuleElement.Element(CollisionShapeXml.Inst.Height).GetFloat();
					Vector3 position = GetPosition(capsuleElement);
					Quaternion rotation = GetRotation(capsuleElement);

					return new Capsule(diameter, height, position, rotation);
				}

				public override void SetTo(CollisionShape shapeComponent)
				{
					shapeComponent.SetCapsule(diameter, height, position, rotation);
				}
			}

			class Cone : ConcreteShape {

				public static XName ElementName => CollisionShapeXml.Inst.Cone;

				readonly float diameter;
				readonly float height;
				readonly Vector3 position;
				readonly Quaternion rotation;

				Cone(float diameter, float height, Vector3 position, Quaternion rotation)
				{
					this.diameter = diameter;
					this.height = height;
					this.position = position;
					this.rotation = rotation;
				}

				public static Cone FromXml(XElement coneElement, GamePack package)
				{
					if (coneElement.Name != ElementName)
					{
						throw new ArgumentException($"Expected element {ElementName}, got {coneElement.Name}", nameof(coneElement));
					}

					float diameter = coneElement.Element(CollisionShapeXml.Inst.Diameter).GetFloat();
					float height = coneElement.Element(CollisionShapeXml.Inst.Height).GetFloat();
					Vector3 position = GetPosition(coneElement);
					Quaternion rotation = GetRotation(coneElement);

					return new Cone(diameter, height, position, rotation);
				}

				public override void SetTo(CollisionShape shapeComponent)
				{
					shapeComponent.SetCone(diameter, height, position, rotation);
				}
			}

			class ConvexHull : ConcreteShape {

				public static XName ElementName => CollisionShapeXml.Inst.ConvexHull;

				readonly Model model;
				readonly uint lodLevel;
				readonly Vector3 scale;
				readonly Vector3 position;
				readonly Quaternion rotation;

				ConvexHull(Model model, uint lodLevel, Vector3 scale, Vector3 position, Quaternion rotation)
				{
					this.model = model;
					this.lodLevel = lodLevel;
					this.scale = scale;
					this.position = position;
					this.rotation = rotation;
				}

				public static ConvexHull FromXml(XElement hullElement, GamePack package)
				{
					if (hullElement.Name != ElementName)
					{
						throw new ArgumentException($"Expected element {ElementName}, got {hullElement.Name}", nameof(hullElement));
					}

					string modelPath = hullElement.Element(CollisionShapeXml.Inst.ModelPath).GetPath();

					Model model = package.PackageManager.GetModel(modelPath);
					uint lodLevel = hullElement.Element(CollisionShapeXml.Inst.Height).GetUInt();
					Vector3 scale = hullElement.Element(CollisionShapeXml.Inst.Scale).GetVector3();
					Vector3 position = GetPosition(hullElement);
					Quaternion rotation = GetRotation(hullElement);

					return new ConvexHull(model, lodLevel, scale, position, rotation);
				}

				public override void SetTo(CollisionShape shapeComponent)
				{
					shapeComponent.SetConvexHull(model, lodLevel, scale, position, rotation);
				}
			}

			class Cylinder : ConcreteShape {

				public static XName ElementName => CollisionShapeXml.Inst.Cylinder;

				readonly float diameter;
				readonly float height;
				readonly Vector3 position;
				readonly Quaternion rotation;

				Cylinder(float diameter, float height, Vector3 position, Quaternion rotation)
				{
					this.diameter = diameter;
					this.height = height;
					this.position = position;
					this.rotation = rotation;
				}

				public static Cylinder FromXml(XElement cylinderElement, GamePack package)
				{
					if (cylinderElement.Name != ElementName)
					{
						throw new ArgumentException($"Expected element {ElementName}, got {cylinderElement.Name}", nameof(cylinderElement));
					}

					float diameter = cylinderElement.Element(CollisionShapeXml.Inst.Diameter).GetFloat();
					float height = cylinderElement.Element(CollisionShapeXml.Inst.Height).GetFloat();
					Vector3 position = GetPosition(cylinderElement);
					Quaternion rotation = GetRotation(cylinderElement);

					return new Cylinder(diameter, height, position, rotation);
				}

				public override void SetTo(CollisionShape shapeComponent)
				{
					shapeComponent.SetCylinder(diameter, height, position, rotation);
				}
			}

			class Sphere : ConcreteShape {

				public static XName ElementName => CollisionShapeXml.Inst.Sphere;

				readonly float diameter;
				readonly Vector3 position;
				readonly Quaternion rotation;

				Sphere(float diameter, Vector3 position, Quaternion rotation)
				{
					this.diameter = diameter;
					this.position = position;
					this.rotation = rotation;
				}

				public static Sphere FromXml(XElement sphereElement, GamePack package)
				{
					if (sphereElement.Name != ElementName) {
						throw new ArgumentException($"Expected element {ElementName}, got {sphereElement.Name}", nameof(sphereElement));
					}

					float diameter = sphereElement.Element(CollisionShapeXml.Inst.Diameter).GetFloat();
					Vector3 position = GetPosition(sphereElement);
					Quaternion rotation = GetRotation(sphereElement);

					return new Sphere(diameter, position, rotation);
				}

				public override void SetTo(CollisionShape shapeComponent)
				{
					shapeComponent.SetSphere(diameter, position, rotation);
				}
			}

			static readonly Dictionary<XName, LoadShapeDelegate> dispatch;

			readonly ConcreteShape shape;

			static CollisionShapeLoader()
			{
				dispatch = new Dictionary<XName, LoadShapeDelegate>
							{
								{Box.ElementName, Box.FromXml },
								{Capsule.ElementName, Capsule.FromXml },
								{Cone.ElementName, Cone.FromXml },
								{ConvexHull.ElementName, ConvexHull.FromXml },
								{Cylinder.ElementName, Cylinder.FromXml },
								{Sphere.ElementName, Sphere.FromXml }
							};
			}

			CollisionShapeLoader(ConcreteShape shape)
			{
				this.shape = shape;
			}

			/// <summary>
			/// Loads collision shape described by the XML <paramref name="element"/>.
			/// </summary>
			/// <param name="element">XML describing the collision shape.</param>
			/// <param name="package">Source of the XML.</param>
			/// <returns></returns>
			/// <exception cref="ArgumentNullException">Thrown when the <paramref name="element"/> is null</exception>
			/// <exception cref="ArgumentException">Throw when the <paramref name="element"/> does not conform to the xml schema</exception>
			public static CollisionShapeLoader Load(XElement element, GamePack package)
			{
				if (element == null)
				{
					throw new ArgumentNullException(nameof(element), "Element was null");
				}


				if (element.Name != AssetsXml.Inst.CollisionShape)
				{
					throw new ArgumentException("Invalid element", nameof(element));
				}

				XElement child = element.Elements().First();

				ConcreteShape newShape;
				if (dispatch.TryGetValue(child.Name, out var loadShape)) {
					newShape = loadShape(child, package);
				}
				else {
					throw new
						ArgumentException($"Element {element} was not valid according to GamePack.xml, unexpected child {child}", nameof(element));
				}
		

				return new CollisionShapeLoader(newShape);
			}

		

			public override void ApplyOn(Node node)
			{
				if (node.GetComponent<RigidBody>() == null) {
					//Rigid body properties are set elsewhere (in the CreateNew methods of entities), to make it independent of the assets type
					node.CreateComponent<RigidBody>();
				}
				CollisionShape shapeComponent = node.CreateComponent<CollisionShape>();
				shape.SetTo(shapeComponent);
			}
		}


		static readonly Dictionary<XName, ParseAssetLoaderDelegate> Parsers;

		readonly List<AssetLoader> loaders;

		static ItemsAssetContainer()
		{
			//TODO: Add other asset parsers
			Parsers = new Dictionary<XName, ParseAssetLoaderDelegate>
					{
						{AssetsXml.Inst.Model, AnimatedModelLoader.Load},
						{AssetsXml.Inst.Scale, ScaleLoader.Load},
						{AssetsXml.Inst.CollisionShape, CollisionShapeLoader.Load }
					};
		
		}

		/// <summary>
		/// Creates container for assets described item by item in the package XML.
		/// </summary>
		/// <param name="assetsElement">The description of the assets.</param>
		/// <param name="package">Source GamePack</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="assetsElement"/> is null or there is internal error</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="assetsElement"/> does not conform t the xml schema</exception>
		/// <exception cref="ResourceLoadingException">Thrown when one of the referenced resources could not be loaded</exception>
		public ItemsAssetContainer(XElement assetsElement, GamePack package)
		{
			CheckWithType(assetsElement,  AssetsXml.ItemsType);

			loaders = new List<AssetLoader>();

			try {
				foreach (var element in assetsElement.Elements()) {
					if (Parsers.TryGetValue(element.Name, out ParseAssetLoaderDelegate parse)) {
						//TODO: Possible exceptions
						loaders.Add(parse(element, package));
					}
				}
			}
			catch (Exception) {
				foreach (var loader in loaders) {
					loader.Dispose();
				}
				throw;
			}
		}

		public override Node Instantiate(ILevelManager level, Vector3 position, Quaternion rotation)
		{
			var newNode = level.LevelNode.CreateChild();
			newNode.Position = position;
			newNode.Rotation = rotation;

			foreach (var loader in loaders) {
				loader.ApplyOn(newNode);
			}

			return newNode;
		}

		public override void Dispose()
		{
			foreach (var loader in loaders)
			{
				loader.Dispose();
			}
		}
	}
}
