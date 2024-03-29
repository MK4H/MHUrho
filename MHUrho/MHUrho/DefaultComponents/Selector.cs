﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MHUrho.Logic;
using MHUrho.Control;
using Urho;

namespace MHUrho.DefaultComponents
{


	public abstract class Selector : DefaultComponent {

		public virtual bool Selected { get; protected set; }

		protected Selector(ILevelManager level)
			:base(level)
		{

		}

		public abstract bool Order(Order order);

		public abstract void Select();

		public abstract void Deselect();

		protected override void AddedToEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			base.AddedToEntity(entityDefaultComponents);
			AddedToEntity(typeof(Selector), entityDefaultComponents);
		}

		protected override bool RemovedFromEntity(IDictionary<Type, IList<DefaultComponent>> entityDefaultComponents) {
			bool removedBase = base.RemovedFromEntity(entityDefaultComponents);
			bool removed = RemovedFromEntity(typeof(Selector), entityDefaultComponents);
			Debug.Assert(removedBase == removed, "DefaultComponent was not correctly registered in the entity");
			return removed;
		}
	}
}
