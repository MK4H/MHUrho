using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Control;
using Urho;

namespace MHUrho.UnitComponents
{
	public class OrderArgs {
		public bool Executed { get; set; }
	}

	public abstract class Selector : DefaultComponent {
		public virtual IPlayer Player { get; }

		public virtual bool Selected { get; protected set; }

		public abstract bool Order(ITile tile, int buttons, int qualifiers);

		public abstract bool Order(Unit unit, int buttons, int qualifiers);

		public abstract bool Order(Building unit, int buttons, int qualifiers);

		public abstract void Select();

		public abstract void Deselect();

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			AddedToEntity(typeof(Selector), entityDefaultComponents);
		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			return RemovedFromEntity(typeof(Selector), entityDefaultComponents);
		}
	}
}
