using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.Logic;

namespace MHUrho.PathFinding
{
	public interface IPathFindAlg {

		/// <summary>
		/// Find a path between the <paramref name="source"/> position and the <paramref name="target"/> node.
		/// From <paramref name="source"/> position, the path will lead to the closest <see cref="INode"/> and then
		/// by a path to the <paramref name="target"/>.
		/// Distances will be calculated by the <paramref name="nodeDistCalculator"/>.
		/// </summary>
		/// <param name="source">Source position, from which the path should start.</param>
		/// <param name="target">Target node, to which the path should lead.</param>
		/// <param name="nodeDistCalculator">Calculator for distances between nodes.</param>
		/// <returns>A path from <paramref name="source"/> position to the <paramref name="target"/> node, or null if path is not possible.</returns>
		Path FindPath(Vector3 source,
					INode target,
					INodeDistCalculator nodeDistCalculator);

		/// <summary>
		/// Gets list of tiles the path crosses when going from <paramref name="source"/> to the <paramref name="target"/> node.
		/// From <paramref name="source"/> position, the path will lead to the closest <see cref="INode"/> and then
		/// by a path to the <paramref name="target"/>.
		/// Distances will be calculated by the <paramref name="nodeDistCalculator"/>
		/// </summary>
		/// <param name="source">Source position, from which the path should start.</param>
		/// <param name="target">Target node, to which the path should lead.</param>
		/// <param name="nodeDistCalculator">Calculator for distances between nodes.</param>
		/// <returns>A list of tiles the path from <paramref name="source"/> to <paramref name="target"/> crosses, or null if a path is not possible.</returns>
		List<ITile> GetTileList(Vector3 source,
								INode target,
								INodeDistCalculator nodeDistCalculator);

		/// <summary>
		/// Gets closest node to the world <paramref name="position"/>. Is used in <see cref="FindPath(Vector3, INode, INodeDistCalculator)"/>
		/// and <see cref="GetTileList(Vector3, INode, INodeDistCalculator)"/> to get the closest node to the source position.
		/// </summary>
		/// <param name="position">Position in the game world.</param>
		/// <returns>A closest node to the <paramref name="position"/>.</returns>
		/// <exception cref="Exception">May throw exception on failure.</exception>
		INode GetClosestNode(Vector3 position);

		/// <summary>
		/// Gets the <see cref="ITileNode"/> corresponding to the given <paramref name="tile"/>.
		/// </summary>
		/// <param name="tile">Tile, of which we want the <see cref="ITileNode"/>.</param>
		/// <returns>The <see cref="ITileNode"/> corresponding to the given <paramref name="tile"/>.</returns>
		/// <exception cref="Exception">May throw exception on failure.</exception>
		ITileNode GetTileNode(ITile tile);

		/// <summary>
		/// Creates a building node in the pathfinding graph associated with the given <paramref name="building"/>
		/// at the given <paramref name="position"/>. Can be tagged by given <paramref name="tag"/> to better differentiate
		/// between nodes of the same <paramref name="building"/>.
		/// </summary>
		/// <param name="building">Building with which the new node should be associated.</param>
		/// <param name="position">Position of the new node in the game world.</param>
		/// <param name="tag">User defined tag that will be stored in the node.</param>
		/// <returns>The newly created node.</returns>
		/// <exception cref="Exception">May throw exception on failure.</exception>
		IBuildingNode CreateBuildingNode(IBuilding building, Vector3 position, object tag);

		/// <summary>
		/// Creates a temporary node, which is not associated with any building or tile.
		/// </summary>
		/// <param name="position">Position of the new node in the game world.</param>
		/// <returns>The newly created node.</returns>
		/// <exception cref="Exception">May throw exception on failure.</exception>
		ITempNode CreateTempNode(Vector3 position);
	}
}
