using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.UnitComponents;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.EntityInfo
{
    public class HealthBar : IDisposable {


		static Dictionary<IPlayer, Material> coloredHealthbars = new Dictionary<IPlayer, Material>();

		BillboardSet billboardSet;
		uint billboardIndex;

		ILevelManager level;

		public HealthBar(ILevelManager level, IEntity entity, Vector3 offset, Vector2 size, float healthPercent)
		{
			this.level = level;
			if (!coloredHealthbars.ContainsKey(entity.Player)) {
				CreateHealthbar(entity.Player);
			}

			AddToEntity(entity, offset, size);
			SetHealth(healthPercent);
		}


		public void SetHealth(float healthPercent)
		{
			healthPercent = Math.Max(Math.Min(healthPercent, 100), 0);

			var billboard = billboardSet.GetBillboardSafe(billboardIndex);
			int imagePart = (int)healthPercent / 5;
			billboard.Uv = new Rect(new Vector2(0, imagePart / 21.0f), new Vector2(1, (imagePart + 1) / 21.0f));

			billboardSet.Commit();
		}

		public void Dispose()
		{
			billboardSet.Dispose();
		}

		public void Show()
		{
			var billboard = billboardSet.GetBillboardSafe(billboardIndex);
			billboard.Enabled = true;
			billboardSet.Commit();
		}

		public void Hide()
		{
			var billboard = billboardSet.GetBillboardSafe(billboardIndex);
			billboard.Enabled = false;
			billboardSet.Commit();
		}

		unsafe void CreateHealthbar(IPlayer player)
		{
			Image image = PackageManager.Instance.GetImage("Textures/HealthBars.png").ConvertToRGBA();

			uint playerColor = player.Color.ToUInt();
			uint* imageData = (uint*)image.Data;
			for (int i = 0; i < image.Width * image.Height; i++, imageData++) {
				if (*imageData == new Color(1, 1, 1).ToUInt()) {
					*imageData = playerColor;
				}
			}

			Material newMaterial = PackageManager.Instance.GetMaterialFromImage(image);
			coloredHealthbars.Add(player, newMaterial);
		}

		void AddToEntity(IEntity entity, Vector3 offset, Vector2 size)
		{
			billboardSet = null;
			foreach (var component in entity.GetComponents<BillboardSet>()) {
				if (component.Material == coloredHealthbars[entity.Player]) {
					billboardSet = component;
					billboardIndex = billboardSet.NumBillboards;
					billboardSet.NumBillboards = billboardSet.NumBillboards + 1;
					
				}
			}

			if (billboardSet == null) {
				billboardSet = entity.CreateComponent<BillboardSet>();

				billboardSet.FaceCameraMode = FaceCameraMode.RotateXyz;
				billboardSet.NumBillboards = 1;
				billboardSet.Sorted = false;
				//TODO: Material gets deallocated after the death of the last unit with this healthbar, fix
				billboardSet.Material = coloredHealthbars[entity.Player];
				billboardSet.Scaled = false;

				billboardIndex = 0;
			}

			


			var billboard = billboardSet.GetBillboardSafe(billboardIndex);
			billboard.Position = offset;
			billboard.Rotation = 0;
			billboard.Size = size;
			billboard.Enabled = true;

		}
	}
}
