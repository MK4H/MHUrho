using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.EntityInfo
{
    public class HealthBar {
		const int healthBarPixelWidth = 100;
		const int healthBarPixelHeight = 20;
		const int horizontalBorderPixelHeight = 6;
		const int verticalBorderPixelWidth = 3;
		const int horizontalDividerPixelHeight = 0;
		const int verticalDividerPixelWidth = 0;

		static readonly Color HealthyColor = new Color(0.1f, 0.8f, 0.1f);
		static readonly Color DeadColor = new Color(0.8f,0.1f,0.1f);
		static readonly uint DividerColor = Color.Black.ToUInt();

		ILevelManager level;
		Image image;
		Texture2D texture;

		BillboardSet billboardSet;

		public HealthBar(ILevelManager level)
		{
			this.level = level;
			image = new Image();
			image.SetSize(healthBarPixelWidth + verticalBorderPixelWidth * 2 + verticalDividerPixelWidth * 2, 
						healthBarPixelHeight + horizontalBorderPixelHeight * 2 + horizontalDividerPixelHeight * 2, 
						4);
		}

		public void AddToNode(Node node, IEntity entity)
		{
			image.Clear(entity.Player.Color);
			

			texture = new Texture2D();
			texture.SetData(image);

			var material = new Material();
			material.Load(level.PackageManager.ResourceCache.GetFile("Materials/HealthBarMat.xml"));
			material.SetTexture(TextureUnit.Diffuse, texture);

			billboardSet = node.CreateComponent<BillboardSet>();
			billboardSet.FaceCameraMode = FaceCameraMode.RotateXyz;
			billboardSet.NumBillboards = 1;
			billboardSet.Sorted = false;
			billboardSet.Material = material;
			billboardSet.Scaled = false;
			

			var billboard = billboardSet.GetBillboardSafe(0);
			billboard.Position = new Vector3(0,1.5f / node.Scale.Y,0);
			billboard.Rotation = 0;
			billboard.Size = new Vector2(0.5f, 0.1f);
			billboard.Uv = new Rect(new Vector2(0, 0), new Vector2(1, 1));
			billboard.Enabled = true;

			InitialDraw(entity.Player.Color, 49);
		}

		public void SetHealth(int healthPercent)
		{
			DrawHealth(healthPercent);
		}

		void InitialDraw(Color playerColor, int healthPercent)
		{
			uint pixelColor = playerColor.ToUInt();
			unsafe {
				uint* imageData = (uint*)image.Data;

				DrawHorizontalBorder(ref imageData, pixelColor);

				//Horizontal divider
				for (int i = 0; i < horizontalDividerPixelHeight; i++) {
					DrawVerticalBorder(ref imageData, pixelColor);
					DrawHorizontalDivider(ref imageData);
					DrawVerticalBorder(ref imageData, pixelColor);
				}

				//Health bar
				for (int i = 0; i < healthBarPixelHeight; i++) {
					DrawVerticalBorder(ref imageData, pixelColor);
					DrawVerticalDivider(ref imageData);
					DrawHealthBarRow(ref imageData, healthPercent);
					DrawVerticalDivider(ref imageData);
					SkipVerticalBorder(ref imageData);
				}

				//Horizontal divider
				for (int i = 0; i < horizontalDividerPixelHeight; i++) {
					DrawVerticalBorder(ref imageData, pixelColor);
					DrawHorizontalDivider(ref imageData);
					DrawVerticalBorder(ref imageData, pixelColor);
				}

				DrawHorizontalBorder(ref imageData, playerColor.ToUInt());
			}
			texture.SetData(image);
		}

		void DrawHealth(int healthPercent)
		{
			unsafe {
				uint* imageData = (uint*) image.Data;

				SkipHorizontalBorder(ref imageData);

				SkipHorizontalDivider(ref imageData);

				for (int i = 0; i < healthBarPixelHeight; i++) {
					SkipVerticalBorder(ref imageData);
					SkipVerticalDivider(ref imageData);

					DrawHealthBarRow(ref imageData, healthPercent);

					SkipVerticalDivider(ref imageData);
					SkipVerticalBorder(ref imageData);
				}
			}

			texture.SetData(image);
		}

		void DrawBorders(Color color)
		{
			uint pixelColor = color.ToUInt();
			unsafe {
				uint* imageData = (uint*)image.Data;
				DrawHorizontalBorder(ref imageData, pixelColor);

				for (int i = 0; i < healthBarPixelHeight + horizontalDividerPixelHeight; i++) {
					DrawVerticalBorder(ref imageData, pixelColor);
					SkipVerticalDivider(ref imageData);
					SkipHealthBarRow(ref imageData);
					SkipVerticalDivider(ref imageData);
					DrawVerticalBorder(ref imageData, pixelColor);
				}

				DrawHorizontalBorder(ref imageData, pixelColor);
			}

			texture.SetData(image);
		}

		unsafe void SkipHorizontalBorder(ref uint* imageData)
		{
			imageData += horizontalBorderPixelHeight * image.Width;
		}

		unsafe void SkipHorizontalDivider(ref uint* imageData)
		{
			imageData += horizontalDividerPixelHeight * image.Width;
		}

		unsafe void SkipVerticalBorder(ref uint* imageData)
		{
			imageData += verticalBorderPixelWidth;
		}

		unsafe void SkipVerticalDivider(ref uint* imageData)
		{
			imageData += verticalDividerPixelWidth;
		}

		unsafe void SkipHealthBarRow(ref uint* imageData)
		{
			imageData += healthBarPixelWidth;
		}

		unsafe void DrawHealthBarRow(ref uint* imageData, int healthPercent)
		{
			int pixelX = 0;
			uint healthyColor = HealthyColor.ToUInt();
			uint deadColor = DeadColor.ToUInt();
			for (; pixelX < (healthPercent / 100.0f) * healthBarPixelWidth; pixelX++) {
				*imageData++ = healthyColor;
			}

			for (; pixelX < healthBarPixelWidth; pixelX++) {
				*imageData++ = deadColor;
			}
			
		}

		unsafe void DrawHorizontalBorder(ref uint* imageData, uint color)
		{
			for (int y = 0; y < horizontalBorderPixelHeight; y++) {
				for (int x = 0; x < image.Width; x++) {
					*imageData++ = color;
				}
			}
		}

		unsafe void DrawVerticalBorder(ref uint* imageData, uint color)
		{
			for (int i = 0; i < verticalBorderPixelWidth; i++) {
				*imageData++ = color;
			}
		}

		unsafe void DrawHorizontalDivider(ref uint* imageData)
		{
			for (int i = 0; i < verticalDividerPixelWidth * 2 + healthBarPixelWidth; i++) {
				*imageData++ = DividerColor;
			}
		}

		unsafe void DrawVerticalDivider(ref uint* imageData)
		{
			for (int i = 0; i < verticalDividerPixelWidth; i++) {
				*imageData++ = DividerColor;
			}
		}
	}
}
