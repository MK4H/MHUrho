using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.PathFinding.AStar
{
	public abstract class NodeDistCalculator : INodeDistCalculator, INodeVisitor {

		bool canPass;
		float resTime;

		public bool GetTime(INode source, INode target, MovementType movementType, out float time)
		{
			source.Accept(this, target, movementType);
			time = resTime;
			return canPass;
		}

		/// <summary>
		/// Used as a heuristic for the A* algorithm
		///
		/// It has to be admissible, which means it must not overestimate the time
		/// GetMinimalAproxTime has to always be lower than the optimal path time
		///
		/// But the closer you get it to the optimal time, the faster the A* will run
		///
		/// If it is not admissible, the returned path may not be optimal and the runtime of A* may be longer
		///
		/// If the <paramref name="source"/> and <paramref name="target"/> are in the same Tile, it should return the optimal time
		/// as that will be used for the movement
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public abstract float GetMinimalAproxTime(Vector3 source, Vector3 target);

		//These methods translate the Visitor API to nicer GetTime virtual methods 

		void INodeVisitor.Visit(ITileNode source, ITileNode target, MovementType movementType)
		{
			canPass = GetTime(source, target, movementType, out resTime);
		}
		void INodeVisitor.Visit(ITileNode source, IBuildingNode target, MovementType movementType)
		{
			canPass = GetTime(source, target, movementType, out resTime);
		}
		void INodeVisitor.Visit(ITileNode source, ITempNode target, MovementType movementType)
		{
			canPass = GetTime(source, target, movementType, out resTime);
		}

		void INodeVisitor.Visit(IBuildingNode source, ITileNode target, MovementType movementType)
		{
			canPass = GetTime(source, target, movementType, out resTime);
		}
		void INodeVisitor.Visit(IBuildingNode source, IBuildingNode target, MovementType movementType)
		{
			canPass = GetTime(source, target, movementType, out resTime);
		}
		void INodeVisitor.Visit(IBuildingNode source, ITempNode target, MovementType movementType)
		{
			canPass = GetTime(source, target, movementType, out resTime);
		}

		void INodeVisitor.Visit(ITempNode source, ITileNode target, MovementType movementType)
		{
			canPass = GetTime(source, target, movementType, out resTime);
		}
		void INodeVisitor.Visit(ITempNode source, IBuildingNode target, MovementType movementType)
		{
			canPass = GetTime(source, target, movementType, out resTime);
		}
		void INodeVisitor.Visit(ITempNode source, ITempNode target, MovementType movementType)
		{
			canPass = GetTime(source, target, movementType, out resTime);
		}

		//These methods are for the user to reimplement

		/// <summary>
		/// Gets if it is possible to get from <paramref name="source"/> TILE node to <paramref name="target"/> TILE node,
		///  if true, then returns the time needed in <paramref name="time"/>
		/// </summary>
		/// <param name="source">Source node</param>
		/// <param name="target">Target node</param>
		/// <param name="movementType">Movement type from <paramref name="source"/> to <paramref name="target"/>.</param>
		/// <param name="time">Result time it will take to get from <paramref name="source"/> to <paramref name="target"/></param>
		/// <returns>If it is possible to get to the</returns>
		protected virtual bool GetTime(ITileNode source, ITileNode target, MovementType movementType, out float time)
		{
			time = -1;
			return false;
		}

		/// <summary>
		/// Gets if it is possible to get from <paramref name="source"/> TEMP node to <paramref name="target"/> BUILDING node,
		///  if true, then returns the time needed in <paramref name="time"/>
		/// </summary>
		/// <param name="source">Source node</param>
		/// <param name="target">Target node</param>
		/// <param name="time">Result time it will take to get from <paramref name="source"/> to <paramref name="target"/></param>
		/// <returns>If it is possible to get to the</returns>
		protected virtual bool GetTime(ITileNode source, IBuildingNode target, MovementType movementType, out float time)
		{
			time = -1;
			return false;
		}

		/// <summary>
		/// Gets if it is possible to get from <paramref name="source"/> TILE node to <paramref name="target"/> TEMP node,
		///  if true, then returns the time needed in <paramref name="time"/>
		/// </summary>
		/// <param name="source">Source node</param>
		/// <param name="target">Target node</param>
		/// <param name="time">Result time it will take to get from <paramref name="source"/> to <paramref name="target"/></param>
		/// <returns>If it is possible to get to the</returns>
		protected virtual bool GetTime(ITileNode source, ITempNode target, MovementType movementType, out float time)
		{
			time = -1;
			return false;
		}

		/// <summary>
		/// Gets if it is possible to get from <paramref name="source"/> BUILDING node to <paramref name="target"/> TILE node,
		///  if true, then returns the time needed in <paramref name="time"/>
		/// </summary>
		/// <param name="source">Source node</param>
		/// <param name="target">Target node</param>
		/// <param name="time">Result time it will take to get from <paramref name="source"/> to <paramref name="target"/></param>
		/// <returns>If it is possible to get to the</returns>
		protected virtual bool GetTime(IBuildingNode source, ITileNode target, MovementType movementType, out float time)
		{
			time = -1;
			return false;
		}

		/// <summary>
		/// Gets if it is possible to get from <paramref name="source"/> BUILDING node to <paramref name="target"/> BUILDING node,
		///  if true, then returns the time needed in <paramref name="time"/>
		/// </summary>
		/// <param name="source">Source node</param>
		/// <param name="target">Target node</param>
		/// <param name="time">Result time it will take to get from <paramref name="source"/> to <paramref name="target"/></param>
		/// <returns>If it is possible to get to the</returns>
		protected virtual bool GetTime(IBuildingNode source, IBuildingNode target, MovementType movementType, out float time)
		{
			time = -1;
			return false;
		}

		/// <summary>
		/// Gets if it is possible to get from <paramref name="source"/> BUILDING node to <paramref name="target"/> TEMP node,
		///  if true, then returns the time needed in <paramref name="time"/>
		/// </summary>
		/// <param name="source">Source node</param>
		/// <param name="target">Target node</param>
		/// <param name="time">Result time it will take to get from <paramref name="source"/> to <paramref name="target"/></param>
		/// <returns>If it is possible to get to the</returns>
		protected virtual bool GetTime(IBuildingNode source, ITempNode target, MovementType movementType, out float time)
		{
			time = -1;
			return false;
		}

		/// <summary>
		/// Gets if it is possible to get from <paramref name="source"/> TEMP node to <paramref name="target"/> TILE node,
		///  if true, then returns the time needed in <paramref name="time"/>
		/// </summary>
		/// <param name="source">Source node</param>
		/// <param name="target">Target node</param>
		/// <param name="time">Result time it will take to get from <paramref name="source"/> to <paramref name="target"/></param>
		/// <returns>If it is possible to get to the</returns>
		protected virtual bool GetTime(ITempNode source, ITileNode target, MovementType movementType, out float time)
		{
			time = -1;
			return false;
		}

		/// <summary>
		/// Gets if it is possible to get from <paramref name="source"/> TEMP node to <paramref name="target"/> BUILDING node,
		///  if true, then returns the time needed in <paramref name="time"/>
		/// </summary>
		/// <param name="source">Source node</param>
		/// <param name="target">Target node</param>
		/// <param name="time">Result time it will take to get from <paramref name="source"/> to <paramref name="target"/></param>
		/// <returns>If it is possible to get to the</returns>
		protected virtual bool GetTime(ITempNode source, IBuildingNode target, MovementType movementType, out float time)
		{
			time = -1;
			return false;
		}

		/// <summary>
		/// Gets if it is possible to get from <paramref name="source"/> TEMP node to <paramref name="target"/> TEMP node,
		///  if true, then returns the time needed in <paramref name="time"/>
		/// </summary>
		/// <param name="source">Source node</param>
		/// <param name="target">Target node</param>
		/// <param name="time">Result time it will take to get from <paramref name="source"/> to <paramref name="target"/></param>
		/// <returns>If it is possible to get to the</returns>
		protected virtual bool GetTime(ITempNode source, ITempNode target, MovementType movementType, out float time)
		{
			time = -1;
			return false;
		}
	}
}
