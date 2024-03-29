﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.WorldMap;
using Urho;
using Urho.Gui;

namespace ShowcasePackage.Buildings
{
	public abstract class Builder : IDisposable {

		public BuildingType BuildingType { get; private set; }

		protected ILevelManager Level { get; private set; }
		protected IMap Map => Level.Map;

		protected Builder(ILevelManager level, BuildingType type)
		{
			this.Level = level;
			this.BuildingType = type;
		}

		public virtual void Enable() {

		}

		public virtual void Disable() {

		}

		public virtual void OnMouseUp(MouseButtonUpEventArgs e) {

		}

		public virtual void OnMouseDown(MouseButtonDownEventArgs e) {

		}

		public virtual void OnMouseMove(MHUrhoMouseMovedEventArgs e) {

		}

		public virtual void OnMouseWheelMoved(MouseWheelEventArgs e) {

		}

		public virtual void OnUpdate(float timeStep) {

		}



		public virtual void UIHoverBegin() {

		}

		public virtual void UIHoverEnd() {

		}

		public virtual void Dispose()
		{

		}

		protected IntRect GetBuildingRectangle(ITile centerTile, BuildingType buildingType)
		{
			IntVector2 topLeft = centerTile.TopLeft - buildingType.Size / 2;
			IntVector2 bottomRight = buildingType.GetBottomRightTileIndex(topLeft);
			Map.SnapToMap(ref topLeft, ref bottomRight);

			return new IntRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
		}

		
	}
}
