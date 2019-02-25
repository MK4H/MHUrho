﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using MHUrho.Logic;

namespace MHUrho.WorldMap
{
	public interface ITileHeightChangeNotifier {
		event Action<IReadOnlyCollection<ITile>> TileHeightsChangedCol;
		event Action<ImmutableHashSet<ITile>> TileHeightsChangedHash;

		void WeakRegisterTileHeightObserver(IColTileHeightObserver observer);
		void WeakUnregisterTileHeightObserver(IColTileHeightObserver observer);

		void WeakRegisterTileHeightObserver(IHashTileHeightObserver observer);
		void WeakUnregisterTileHeightObserver(IHashTileHeightObserver observer);
	}

	class TileHeightChangeNotifier : ITileHeightChangeNotifier
	{
		/// <summary>
		/// Occurs when height of any tile in the map changes
		/// Gets called with the changed tile as arguments
		/// </summary>
		public event Action<IReadOnlyCollection<ITile>> TileHeightsChangedCol;

		public event Action<ImmutableHashSet<ITile>> TileHeightsChangedHash;

		readonly List<WeakReference<IColTileHeightObserver>> weakColObservers;
		readonly List<WeakReference<IHashTileHeightObserver>> weakHashObservers;

		public TileHeightChangeNotifier()
		{
			weakColObservers = new List<WeakReference<IColTileHeightObserver>>();
			weakHashObservers = new List<WeakReference<IHashTileHeightObserver>>();
		}
		public void WeakRegisterTileHeightObserver(IColTileHeightObserver observer)
		{
			weakColObservers.RemoveAll(weakRef => !weakRef.TryGetTarget(out var target));
			weakColObservers.Add(new WeakReference<IColTileHeightObserver>(observer));
		}

		public void WeakUnregisterTileHeightObserver(IColTileHeightObserver observer)
		{
			weakColObservers.RemoveAll(weakRef => !weakRef.TryGetTarget(out IColTileHeightObserver target) ||
													observer == target);

		}

		public void WeakRegisterTileHeightObserver(IHashTileHeightObserver observer)
		{
			weakHashObservers.RemoveAll(weakRef => !weakRef.TryGetTarget(out var target));
			weakHashObservers.Add(new WeakReference<IHashTileHeightObserver>(observer));
		}

		public void WeakUnregisterTileHeightObserver(IHashTileHeightObserver observer)
		{
			weakHashObservers.RemoveAll(weakRef => !weakRef.TryGetTarget(out IHashTileHeightObserver target) ||
													observer == target);

		}

		public void Notify(IReadOnlyCollection<ITile> changedTiles)
		{
			ImmutableHashSet<ITile> changedTilesSet = ImmutableHashSet.CreateRange(changedTiles);

			TileHeightsChangedCol?.Invoke(changedTiles);
			TileHeightsChangedHash?.Invoke(changedTilesSet);

			foreach (var weakObserver in weakColObservers) {
				if (weakObserver.TryGetTarget(out var observer)) {
					observer.TileHeightsChanged(changedTiles);
				}
			}

			foreach (var weakObserver in weakHashObservers)
			{
				if (weakObserver.TryGetTarget(out var observer))
				{
					observer.TileHeightsChanged(changedTilesSet);
				}
			}
		}

	}
}
