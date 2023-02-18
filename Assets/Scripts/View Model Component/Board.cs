﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Board : MonoBehaviour 
{
	#region Fields / Properties
	[SerializeField] public GameObject tilePrefab;
	[SerializeField] public GameObject wallPrefab;
	public Dictionary<Point, Tile> tiles = new Dictionary<Point, Tile>();
	public Point min { get { return _min; }}
	public Point max { get { return _max; }}
	Point _min;
	Point _max;
	Point[] dirs = new Point[4]
	{
		new Point(0, 1),
		new Point(0, -1),
		new Point(1, 0),
		new Point(-1, 0)
	};
	Color selectedTileColor = new Color(0, 1, 1, 1);
	Color defaultTileColor = new Color(1, 1, 1, 1);
	#endregion

	#region Public
	public void Load (LevelData data)
	{
		_min = new Point(int.MaxValue, int.MaxValue);
		_max = new Point(int.MinValue, int.MinValue);
		
		for (int i = 0; i < data.tiles.Count; ++i)
		{
			GameObject tileInstance = Instantiate(tilePrefab) as GameObject;
			tileInstance.transform.SetParent(transform);
			Tile t = tileInstance.GetComponent<Tile>();
			t.Load(data.tiles[i]);
			tiles.Add(t.pos, t);
			foreach (WallData wd in data.tiles[i].wallData)
			{
				GameObject wallInstance = Instantiate(wallPrefab) as GameObject;
				wallInstance.transform.SetParent(transform);
				Wall w = wallInstance.GetComponent<Wall>();
				w.Load(t, wd);
				t.walls[wd.direction] = w;
			}

			_min.x = Mathf.Min(_min.x, t.pos.x);
			_min.y = Mathf.Min(_min.y, t.pos.y);
			_max.x = Mathf.Max(_max.x, t.pos.x);
			_max.y = Mathf.Max(_max.y, t.pos.y);
		}
	}

	public Tile GetTile (Point p)
	{
		return tiles.ContainsKey(p) ? tiles[p] : null;
	}

	public List<Tile> Search (Tile start, Func<Tile, Tile, bool> addTile)
	{
		List<Tile> retValue = new List<Tile>();

		// We prime our search by deciding which tile to start from.
		// The tile is added to a queue of tiles for checking now.
		// This first tile has a distance of 0,
		// and no prev tile which indicates that it is the beginning of the path.
		retValue.Add(start);

		ClearSearch();
		Queue<Tile> checkNext = new Queue<Tile>();
		Queue<Tile> checkNow = new Queue<Tile>();

		start.distance = 0;
		checkNow.Enqueue(start);

		// In a loop, we dequeue a tile from the queue of tiles to check this round.
		while (checkNow.Count > 0)
		{
			Tile t = checkNow.Dequeue();
			for (int i = 0; i < 4; ++i)
			{
				// Then we grab a reference to the tiles in each cardinal direction from the current tile
				// and add them to a queue for checking in the future.
				Tile next = GetTile(t.pos + dirs[i]);
				if (next == null || next.distance <= t.distance + 1)
					continue;

				if (addTile(t, next))
				{
					// Any tiles which are added have their distance set to 1 greater than the current tile’s distance.
                    // The current tile is also set as their prev tile reference.
					next.distance = t.distance + 1;
					next.prev = t;
					checkNext.Enqueue(next);
					retValue.Add(next);
				}
			}

			// The current tile is marked as analyzed.
            // There are no more tiles in the queue for checking now, so we will swap queues.
			if (checkNow.Count == 0)
				SwapReference(ref checkNow, ref checkNext);
		}

		return retValue;
	}

	public List<Tile> FindPath(Tile start, Tile end)
    {
		List<Tile> retValue = new List<Tile>();

		List<Tile> openSet = new List<Tile>();
		HashSet<Tile> closedSet = new HashSet<Tile>();
		openSet.Add(start);

		// LOOP
		while (openSet.Count > 0)
		{
			Tile currentTile = openSet[0];
			// current_cell = cell in OPEN_LIST with the lowest F_COST
			for (int i = 1; i < openSet.Count; i++)
			{
				if (openSet[i].f < currentTile.f || openSet[i].f == currentTile.f && openSet[i].h < currentTile.h)
				{
					currentTile = openSet[i];
				}
			}
			openSet.Remove(currentTile);
			closedSet.Add(currentTile);

			if (currentTile == end)
			{
				return RetracePath(start, end);
			}

			foreach (Tile neighbour in GetNeighbours(currentTile.pos.x, currentTile.pos.y, max.x, max.y))
			{
				if (WallSeparatingTiles(currentTile, neighbour) != null || closedSet.Contains(neighbour)) continue;

				int newMovementCostToNeighbour = currentTile.g + GetDistance(currentTile, neighbour);
				if (newMovementCostToNeighbour < neighbour.g || !openSet.Contains(neighbour))
				{
					neighbour.g = newMovementCostToNeighbour;
					neighbour.h = GetDistance(neighbour, end);
					neighbour.prev = currentTile;

					if (!openSet.Contains(neighbour))
						openSet.Add(neighbour);
				}
			}
		}

		return retValue;
	}

	public int GetDistance(Tile tileA, Tile tileB)
	{
		int dstX = Mathf.Abs(tileA.pos.x - tileB.pos.x);
		int dstY = Mathf.Abs(tileA.pos.y - tileB.pos.y);

		if (dstX > dstY)
			return 14 * dstY + 10 * (dstX - dstY);
		return 14 * dstX + 10 * (dstY - dstX);
	}

	List<Tile> RetracePath(Tile startTile, Tile targetTile)
	{
		List<Tile> path = new List<Tile>();
		Tile currentTile = targetTile;

		while (currentTile != startTile)
		{
			path.Add(currentTile);
			currentTile = currentTile.prev;
			SelectTiles(path);
		}

		path.Reverse();
		SelectTiles(path); // TODO remove this
		return path;
	}

	public List<Tile> GetNeighbours(int x, int y, int width, int height)
	{
		List<Tile> myNeighbours = new List<Tile>();

		if (x > 0 && x < width - 1)
		{
			if (y > 0 && y < height - 1)
			{
				if (tiles.ContainsKey(new Point(x + 1, y)))
				{
					Tile wt1 = tiles[new Point(x + 1, y)];
					if (wt1 != null) myNeighbours.Add(wt1);
				}

				if (tiles.ContainsKey(new Point(x - 1, y)))
				{
					Tile wt2 = tiles[new Point(x - 1, y)];
					if (wt2 != null) myNeighbours.Add(wt2);
				}

				if (tiles.ContainsKey(new Point(x, y + 1)))
				{
					Tile wt3 = tiles[new Point(x, y + 1)];
					if (wt3 != null) myNeighbours.Add(wt3);
				}

				if (tiles.ContainsKey(new Point(x, y - 1)))
				{
					Tile wt4 = tiles[new Point(x, y - 1)];
					if (wt4 != null) myNeighbours.Add(wt4);
				}
			}
			else if (y == 0)
			{
				if (tiles.ContainsKey(new Point(x + 1, y)))
				{
					Tile wt1 = tiles[new Point(x + 1, y)];
					if (wt1 != null) myNeighbours.Add(wt1);
				}

				if (tiles.ContainsKey(new Point(x - 1, y)))
				{
					Tile wt2 = tiles[new Point(x - 1, y)];
					if (wt2 != null) myNeighbours.Add(wt2);
				}

				if (tiles.ContainsKey(new Point(x, y + 1)))
				{
					Tile wt3 = tiles[new Point(x, y + 1)];
					if (wt3 != null) myNeighbours.Add(wt3);
				}
			}
			else if (y == height - 1)
			{
				if (tiles.ContainsKey(new Point(x, y - 1)))
				{
					Tile wt4 = tiles[new Point(x, y - 1)];
					if (wt4 != null) myNeighbours.Add(wt4);
				}
				if (tiles.ContainsKey(new Point(x + 1, y)))
				{
					Tile wt1 = tiles[new Point(x + 1, y)];
					if (wt1 != null) myNeighbours.Add(wt1);
				}

				if (tiles.ContainsKey(new Point(x - 1, y)))
				{
					Tile wt2 = tiles[new Point(x - 1, y)];
					if (wt2 != null) myNeighbours.Add(wt2);
				}
			}
		}
		else if (x == 0)
		{
			if (y > 0 && y < height - 1)
			{
				if (tiles.ContainsKey(new Point(x + 1, y)))
				{
					Tile wt1 = tiles[new Point(x + 1, y)];
					if (wt1 != null) myNeighbours.Add(wt1);
				}

				if (tiles.ContainsKey(new Point(x, y - 1)))
				{
					Tile wt4 = tiles[new Point(x, y - 1)];
					if (wt4 != null) myNeighbours.Add(wt4);
				}

				if (tiles.ContainsKey(new Point(x, y + 1)))
				{
					Tile wt3 = tiles[new Point(x, y + 1)];
					if (wt3 != null) myNeighbours.Add(wt3);
				}
			}
			else if (y == 0)
			{
				if (tiles.ContainsKey(new Point(x + 1, y)))
				{
					Tile wt1 = tiles[new Point(x + 1, y)];
					if (wt1 != null) myNeighbours.Add(wt1);
				}

				if (tiles.ContainsKey(new Point(x, y + 1)))
				{
					Tile wt3 = tiles[new Point(x, y + 1)];
					if (wt3 != null) myNeighbours.Add(wt3);
				}
			}
			else if (y == height - 1)
			{
				if (tiles.ContainsKey(new Point(x + 1, y)))
				{
					Tile wt1 = tiles[new Point(x + 1, y)];
					if (wt1 != null) myNeighbours.Add(wt1);
				}

				if (tiles.ContainsKey(new Point(x, y - 1)))
				{
					Tile wt4 = tiles[new Point(x, y - 1)];
					if (wt4 != null) myNeighbours.Add(wt4);
				}
			}
		}
		else if (x == width - 1)
		{
			if (y > 0 && y < height - 1)
			{
				if (tiles.ContainsKey(new Point(x - 1, y)))
				{
					Tile wt2 = tiles[new Point(x - 1, y)];
					if (wt2 != null) myNeighbours.Add(wt2);
				}

				if (tiles[new Point(x, y + 1)] != null)
				{
					Tile wt3 = tiles[new Point(x, y + 1)];
					if (wt3 != null) myNeighbours.Add(wt3);
				}

				if (tiles.ContainsKey(new Point(x, y - 1)))
				{
					Tile wt4 = tiles[new Point(x, y - 1)];
					if (wt4 != null) myNeighbours.Add(wt4);
				}
			}
			else if (y == 0)
			{
				if (tiles.ContainsKey(new Point(x - 1, y)))
				{
					Tile wt2 = tiles[new Point(x - 1, y)];
					if (wt2 != null) myNeighbours.Add(wt2);
				}
				if (tiles[new Point(x, y + 1)] != null)
				{
					Tile wt3 = tiles[new Point(x, y + 1)];
					if (wt3 != null) myNeighbours.Add(wt3);
				}
			}
			else if (y == height - 1)
			{
				if (tiles.ContainsKey(new Point(x - 1, y)))
				{
					Tile wt2 = tiles[new Point(x - 1, y)];
					if (wt2 != null) myNeighbours.Add(wt2);
				}

				if (tiles.ContainsKey(new Point(x, y - 1)))
				{
					Tile wt4 = tiles[new Point(x, y - 1)];
					if (wt4 != null) myNeighbours.Add(wt4);
				}
			}
		}

		return myNeighbours;
	}

	public Wall WallSeparatingTiles(Tile tile1, Tile tile2)
	{
		List<Wall> potentialWalls = new List<Wall>();
		List<Direction> directions1 = tile1.GetDirections(tile2);
		List<Direction> directions2 = tile2.GetDirections(tile1);

		foreach(Direction dir in directions1) {
			if (tile1.walls.ContainsKey(dir))
				potentialWalls.Add(tile1.walls[dir]);
		}

		foreach(Direction dir in directions2) {
			if (tile2.walls.ContainsKey(dir))
				potentialWalls.Add(tile2.walls[dir]);
		}

        bool doTilesShareAnAxis = tile1.pos.x == tile2.pos.x || tile1.pos.y == tile2.pos.y;
        if (!doTilesShareAnAxis)
        {
			// Extra wall checks
			foreach(Direction dir in directions1) {
				Point pointAdjacent = tile1.pos + dir.GetNormal();
				Tile tileAdjacent = GetTile(pointAdjacent);
				if (tileAdjacent != null) {
					Direction dirOpp = dir.GetOpposite();
					if (tileAdjacent.walls.ContainsKey(dirOpp)) {
						potentialWalls.Add(tileAdjacent.walls[dirOpp]);
					}
				}
			}
			foreach(Direction dir in directions2) {
				Point pointAdjacent = tile2.pos + dir.GetNormal();
				Tile tileAdjacent = GetTile(pointAdjacent);
				if (tileAdjacent != null) {
					Direction dirOpp = dir.GetOpposite();
					if (tileAdjacent.walls.ContainsKey(dirOpp)) {
						potentialWalls.Add(tileAdjacent.walls[dirOpp]);
					}
				}
			}
		}
        return potentialWalls.Count > 0 ? potentialWalls.First() : null;
	}

	// TODO: This takes in a lot of repeated logic from the AwarenessController. Is there some way to share it?
	public Unit UnitImpedingMissile(Tile missileSource, Point end)
	{
		Point pos = missileSource.pos;

		// A line algorithm borrowed from Rosetta Code http://rosettacode.org/wiki/Bitmap/Bresenham%27s_line_algorithm#C.23
		int dx = Mathf.Abs(end.x - pos.x);
		int sx = pos.x < end.x ? 1 : -1;
		int dy = Mathf.Abs(end.y - pos.y);
		int sy = pos.y < end.y ? 1 : -1;
		int err = (dx > dy ? dx : -dy) / 2;
		int e2;
		Tile fromTile = null;
		for (; ; )
		{
			Tile tile = GetTile(pos);
			if (tile != null)
			{
				if (fromTile != null)
				{
					GameObject occupant = tile.occupant;
					if (occupant != null)
					{
						Unit newTarget = occupant.GetComponent<Unit>();
						if (newTarget != null)
							return newTarget;
					}
				}
				fromTile = tile;
			}
			e2 = err;
			if (e2 > -dx) { err -= dy; pos.x += sx; }
			if (e2 < dy) { err += dx; pos.y += sy; }
			if (pos == end) break;
		}
		return null;
	}

	public Wall WallImpedingMissile(Tile missileSource, Point end) {
		Point pos = missileSource.pos;

		// A line algorithm borrowed from Rosetta Code http://rosettacode.org/wiki/Bitmap/Bresenham%27s_line_algorithm#C.23
		int dx = Mathf.Abs(end.x - pos.x);
		int sx = pos.x < end.x ? 1 : -1;
		int dy = Mathf.Abs(end.y - pos.y);
		int sy = pos.y < end.y ? 1 : -1;
		int err = (dx > dy ? dx : -dy) / 2;
		int e2;
		Tile fromTile = null;
		for (; ; )
		{
			Tile tile = GetTile(pos);
			if (tile != null)
			{
				if (fromTile != null)
				{
					Wall wall = WallSeparatingTiles(fromTile, tile);
					if (wall != null) {
						return wall;
					}
				}
				fromTile = tile;
			}
			if (pos == end) break; // <<< Different from above method. Should that change, too?
			e2 = err;
			if (e2 > -dx) { err -= dy; pos.x += sx; }
			if (e2 < dy) { err += dx; pos.y += sy; }
		}
		return null;
	}

	public void SelectTiles (List<Tile> tiles)
	{
		SelectTiles(tiles, selectedTileColor);
	}

	public void SelectTiles (List<Tile> tiles, Color color)
	{
		for (int i = tiles.Count - 1; i >= 0; --i)
			tiles[i].GetComponent<Renderer>().material.SetColor("_Color", color);
	}

	public void DeSelectTiles (List<Tile> tiles)
	{
		for (int i = tiles.Count - 1; i >= 0; --i)
			tiles[i].GetComponent<Renderer>().material.SetColor("_Color", defaultTileColor);
	}

	public void DeSelectAllTiles ()
	{
		DeSelectTiles(tiles.Values.ToList());
	}
	#endregion

	#region Private
	void ClearSearch ()
	{
		foreach (Tile t in tiles.Values)
		{
			t.prev = null;
			t.distance = int.MaxValue;
		}
	}

	void SwapReference (ref Queue<Tile> a, ref Queue<Tile> b)
	{
		Queue<Tile> temp = a;
		a = b;
		b = temp;
	}
	#endregion
}