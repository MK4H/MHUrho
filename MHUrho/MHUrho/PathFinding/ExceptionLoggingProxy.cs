using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;

namespace MHUrho.PathFinding
{
	/// <summary>
	/// Logs and ignores any exceptions thrown by the IPathFindAlg
	/// </summary>
	class ExceptionLoggingProxy : IPathFindAlg
	{
		readonly IPathFindAlg actualAlg;

		public ExceptionLoggingProxy(IPathFindAlg pathFindAlg)
		{
			this.actualAlg = pathFindAlg;
		}

		public Path FindPath(Vector3 source, INode target, INodeDistCalculator nodeDistCalculator)
		{
			try {
				return actualAlg.FindPath(source, target, nodeDistCalculator);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Error, $"There was un unexpected exception in {nameof(actualAlg.FindPath)}: {e.Message}");
				return null;
			}
		}

		public List<ITile> GetTileList(Vector3 source, INode target, INodeDistCalculator nodeDistCalculator)
		{
			try
			{
				return actualAlg.GetTileList(source, target, nodeDistCalculator);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Error, $"There was un unexpected exception in {nameof(actualAlg.GetTileList)}: {e.Message}");
				return null;
			}
			
		}

		public INode GetClosestNode(Vector3 position)
		{
			try
			{
				return actualAlg.GetClosestNode(position);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning, $"{nameof(actualAlg.GetClosestNode)} failed with exception: {e.Message}");
				throw;
			}
			
		}

		public ITileNode GetTileNode(ITile tile)
		{
			try
			{
				return actualAlg.GetTileNode(tile);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning, $"{nameof(actualAlg.GetTileNode)} failed with exception: {e.Message}");
				throw;
			}
			
		}

		public IBuildingNode CreateBuildingNode(IBuilding building, Vector3 position, object tag)
		{
			try
			{
				return actualAlg.CreateBuildingNode(building, position, tag);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning, $"{nameof(actualAlg.CreateBuildingNode)} failed with exception: {e.Message}");
				throw;
			}
			
		}

		public ITempNode CreateTempNode(Vector3 position)
		{
			try
			{
				return actualAlg.CreateTempNode(position);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Warning, $"{nameof(actualAlg.CreateTempNode)} failed with exception: {e.Message}");
				throw;
			}
			
		}
	}
}
