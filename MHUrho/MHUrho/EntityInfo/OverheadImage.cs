using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.EntityInfo
{
    public class OverheadImage : IDisposable
    {
		ILevelManager level;
		Material material;
		BillboardSet billboardSet;



		public OverheadImage(ILevelManager level, IEntity entity, Vector3 offset, Vector2 size, string picture)
			: this(level, entity, offset, size, level.PackageManager.GetImage(picture))
		{

		}

		public OverheadImage(ILevelManager level, IEntity entity, Vector3 offset, Vector2 size, Image image)
		{
			this.level = level;
			material = level.PackageManager.GetMaterialFromImage(image);

			AddToEntity(entity, offset, size);
		}

		public void ChangeImage(Image newImage)
		{
			Texture2D texture = new Texture2D();
			texture.SetData(newImage);
			material.SetTexture(TextureUnit.Diffuse, texture);
		}

		public void Dispose()
		{
			material.Dispose();
			billboardSet.Dispose();
		}

		void AddToEntity(IEntity entity, Vector3 offset, Vector2 size)
		{

			billboardSet = entity.Node.CreateComponent<BillboardSet>();
			billboardSet.FaceCameraMode = FaceCameraMode.RotateXyz;
			billboardSet.NumBillboards = 1;
			billboardSet.Sorted = false;
			billboardSet.Material = material;
			billboardSet.Scaled = false;


			var billboard = billboardSet.GetBillboardSafe(0);
			billboard.Position = offset;
			billboard.Rotation = 0;
			billboard.Size = size;
			billboard.Uv = new Rect(new Vector2(0, 0), new Vector2(1, 1));
			billboard.Enabled = true;

		}
	}
}
