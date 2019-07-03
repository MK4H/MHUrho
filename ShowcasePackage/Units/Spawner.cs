using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.WorldMap;
using ShowcasePackage.Misc;
using Urho;

namespace ShowcasePackage.Units
{
	public abstract class Spawner : IDisposable
	{
		public UnitType UnitType { get; private set;}

		public abstract Cost Cost { get; }

		protected ILevelManager Level { get; private set; }
		protected IMap Map => Level.Map;

		protected Spawner(ILevelManager level, UnitType type)
		{
			this.Level = level;
			this.UnitType = type;
		}

		public virtual void Enable()
		{

		}

		public virtual void Disable()
		{

		}

		public virtual void OnMouseUp(MouseButtonUpEventArgs e)
		{

		}

		public virtual void OnMouseDown(MouseButtonDownEventArgs e)
		{

		}

		public virtual void OnMouseMove(MHUrhoMouseMovedEventArgs e)
		{

		}

		public virtual void OnMouseWheelMoved(MouseWheelEventArgs e)
		{

		}

		public virtual void OnUpdate(float timeStep)
		{

		}

		public virtual void UIHoverBegin()
		{

		}

		public virtual void UIHoverEnd()
		{

		}

		public abstract IUnit SpawnAt(ITile tile, IPlayer player);

		public virtual void Dispose()
		{

		}
	}
}
