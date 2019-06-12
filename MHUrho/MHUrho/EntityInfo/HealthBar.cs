using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.DefaultComponents;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.EntityInfo
{
    public class HealthBar : IDisposable {


		BillboardSet billboardSet;
		uint billboardIndex;

		IEntity entity;

		ILevelManager level;

		public HealthBar(ILevelManager level, IEntity entity, Vector3 offset, Vector2 size, float healthPercent)
		{
			this.level = level;
			this.entity = entity;

			AddToEntity(entity, offset, size);
			SetHealth(healthPercent);
		}


		public void SetHealth(float healthPercent)
		{
			healthPercent = Math.Max(Math.Min(healthPercent, 100), 0);

			var billboard = billboardSet.GetBillboardSafe(billboardIndex);
			int imageIndex = (int)healthPercent / entity.Player.Insignia.HealthBarStepSize;

			Rect uv = entity.Player.Insignia.HealthBarFullUv;
			float offset = imageIndex / ((100.0f / entity.Player.Insignia.HealthBarStepSize) + 1);
			uv.Min.Y += offset;
			uv.Max.Y += offset;
			billboard.Uv = uv;

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

		void AddToEntity(IEntity entity, Vector3 offset, Vector2 size)
		{
			billboardSet = null;
			foreach (var component in entity.GetComponents<BillboardSet>()) {
				if (component.Material == entity.Player.Insignia.HealthBarMat) {
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
				billboardSet.Material = entity.Player.Insignia.HealthBarMat;
				billboardSet.Scaled = false;
				//TODO: BILLBOARD DRAW DISTANCE
				billboardSet.DrawDistance = 50;

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
