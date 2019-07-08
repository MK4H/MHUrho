using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;

namespace ShowcasePackage.Misc
{
	public class Cost : IReadOnlyDictionary<ResourceType, double> {

		public int Count => costs.Count;

		public double this[ResourceType key] => costs[key];

		public IEnumerable<ResourceType> Keys => costs.Keys;
		public IEnumerable<double> Values => costs.Values;

		readonly Dictionary<ResourceType, double> costs;

		public static Cost FromXml(XElement costElem, GamePack package)
		{
			var costs = new Dictionary<ResourceType, double>();
			foreach (var child in costElem.Elements()) {
				string resourceName = child.Name.LocalName;
				float resourceAmount = float.Parse(child.Value);

				ResourceType resourceType = package.GetResourceType(resourceName);
				costs.Add(resourceType, resourceAmount);
			}

			return new Cost(costs);
		}

		public Cost(Dictionary<ResourceType, double> costs)
		{
			this.costs = costs;
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			string sep = "";
			foreach (var cost in costs)
			{
				builder.Append($"{sep}{cost.Value} {cost.Key.Name}");
				sep = ",";
			}

			return builder.ToString();
		}

		public IEnumerator<KeyValuePair<ResourceType, double>> GetEnumerator()
		{
			return costs.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		
		public bool ContainsKey(ResourceType key)
		{
			return costs.ContainsKey(key);
		}

		public bool TryGetValue(ResourceType key, out double value)
		{
			return costs.TryGetValue(key, out value);
		}

		public bool HasResources(IPlayer player)
		{
			foreach (var cost in costs) {
				if (player.GetResourceAmount(cost.Key) < cost.Value) {
					return false;
				}
			}
			return true;
		}

		public void TakeFrom(IPlayer player)
		{
			foreach (var cost in costs) {
				player.ChangeResourceAmount(cost.Key, -cost.Value);
			}
		}

		public bool TryTakeFrom(IPlayer player)
		{
			if (!HasResources(player)) {
				return false;
			}

			TakeFrom(player);
			return true;

		}
	}
}
