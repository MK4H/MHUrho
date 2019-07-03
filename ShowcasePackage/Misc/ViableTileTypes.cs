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
	public class ViableTileTypes : IReadOnlyCollection<TileType> {

		public int Count => tileTypes.Count;

		readonly HashSet<TileType> tileTypes;

		public static ViableTileTypes FromXml(XElement canBuildOnElem, GamePack package)
		{
			var tileTypes = new HashSet<TileType>();
			foreach (var child in canBuildOnElem.Elements())
			{
				string tileTypeName = child.Name.LocalName;

				TileType tileType = package.GetTileType(tileTypeName);
				tileTypes.Add(tileType);
			}

			return new ViableTileTypes(tileTypes);
		}

		public ViableTileTypes(HashSet<TileType> tileTypes)
		{
			this.tileTypes = tileTypes;
		}

		public bool CanBuildOn(ITile tile)
		{
			return tileTypes.Contains(tile.Type);
		}

		public IEnumerator<TileType> GetEnumerator()
		{
			return tileTypes.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

	}
}
