using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Board : MonoBehaviour 
{
	#region Fields / Properties
	[SerializeField] public GameObject tilePrefab;
	[SerializeField] public GameObject wallPrefab;
	[SerializeField] public GameObject exitPrefab;
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
	public Color moveRangeHighlightColor = new Color(0, 1, 1, 1);
	public Color targetRangeHighlightColor = new Color(0, 1, 1, 1);
	public Color targetAreaHighlightColor = new Color(0, 1, 1, 1);
	public Color viewingRangeHighlightColor = new Color(0, 1, 1, 1);
	public Color viewingRangeEdgeHighlightColor = new Color(0, 1, 1, 1);
	public Color defaultTileColor = new Color(1, 1, 1, 1);
	#endregion

	void Awake ()
	{
		GameObject.Find("Battle Controller").GetComponent<BattleController>();
		InputController.submitEvent += OnSubmit;
	}

	bool isPaused = false;
	void OnSubmit ()
	{
		isPaused = false;
	}

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

		for (int i = 0; i < data.exits.Count; ++i)
		{
			Point exitPoint = data.exits[i];
			GameObject exitInstance = Instantiate(exitPrefab) as GameObject;
			exitInstance.transform.SetParent(transform);
			ExitMarker e = exitInstance.GetComponent<ExitMarker>();
			e.position = exitPoint;
			e.height = tiles[exitPoint].height;
			e.Match();
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

	public IEnumerator FindPath(Unit unit, Tile start, Tile end, Action<List<Tile>> completionHandler)
    {
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

			HighlightTiles(new List<Tile>{currentTile}, Color.green);

			if (currentTile == end)
			{
				completionHandler(RetracePath(start, end));
				break;
			}

			foreach (Tile neighbour in GetNeighbours(currentTile.pos.x, currentTile.pos.y, max.x, max.y))
			{
				isPaused = GameConfig.Main.DebugPathfinding;

				if (WallSeparatingTiles(currentTile, neighbour) != null || closedSet.Contains(neighbour)) continue;

				HighlightTiles(new List<Tile>{neighbour}, Color.blue);

				int newMovementCostToNeighbour = (currentTile.g + GetDistance(currentTile, neighbour)) * MovementCostMultiplier(unit, neighbour);
				if (newMovementCostToNeighbour < neighbour.g || !openSet.Contains(neighbour))
				{
					neighbour.g = newMovementCostToNeighbour;
					neighbour.h = GetDistance(neighbour, end);
					neighbour.prev = currentTile;

					if (!openSet.Contains(neighbour)) {
						HighlightTiles(new List<Tile>{neighbour}, Color.cyan);
						openSet.Add(neighbour);
					}
				}

				Console.Main.Log(string.Format("{0} - G: {1}, H: {2}, F: {3}", neighbour, neighbour.g, neighbour.h, neighbour.f));

				while(isPaused)  {
					yield return null;
				}
			}
			HighlightTiles(new List<Tile>{currentTile}, Color.red);
		}

		DeHighlightAllTiles();
		yield break;
	}

	public int GetDistance(Tile tileA, Tile tileB)
	{
		int dstX = Mathf.Abs(tileA.pos.x - tileB.pos.x);
		int dstY = Mathf.Abs(tileA.pos.y - tileB.pos.y);

		if (dstX > dstY)
			return 14 * dstY + 10 * (dstX - dstY);
		return 14 * dstX + 10 * (dstY - dstX);
	}

	public int MovementCostMultiplier(Unit unit, Tile tile) {
		GameObject potentialOccupant = tile.occupant;
		if (potentialOccupant != null && potentialOccupant.GetComponent<Unit>() != null && potentialOccupant.GetComponent<Unit>() != unit)
			return 2;
		return 1;
	}

	List<Tile> RetracePath(Tile startTile, Tile targetTile)
	{
		List<Tile> path = new List<Tile>();
		Tile currentTile = targetTile;

		while (currentTile != startTile)
		{
			path.Add(currentTile);
			currentTile = currentTile.prev;
			HighlightTiles(path, BoardColorType.targetRangeHighlight);
		}

		path.Reverse();
		DeHighlightAllTiles(); // TODO remove this
		return path;
	}

	public List<Tile> GetNeighbours(int x, int y, int width, int height)
	{
		List<Tile> myNeighbours = new List<Tile>();

		if (x >= 0 && x <= width)
		{
			if (y >= 0 && y <= height)
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
			else if (y == height)
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
			if (y >= 0 && y <= height)
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
			else if (y == height)
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
		else if (x == width)
		{
			if (y >= 0 && y <= height)
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
			else if (y == height)
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
						if (newTarget != null && newTarget.KO == null)
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

	public void HighlightTiles (List<Tile> tiles, BoardColorType type)
	{
		HighlightTiles(tiles, ColorForType(type));
	}

	public void HighlightTiles (List<Tile> tiles, Color color)
	{
		for (int i = 0; i < tiles.Count; ++i) {
			Tile tile = tiles[i];
			if (tile == null)
				continue;
			tile.SetHighlightColor(color);
		}
	}

	public void DeHighlightTiles (List<Tile> tiles)
	{
		for (int i = 0; i < tiles.Count; ++i) {
			Tile tile = tiles[i];
			if (tile == null)
				continue;
			tile.HideHighlightColor();
		}
	}

	public void DeHighlightAllTiles ()
	{
		DeHighlightTiles(tiles.Values.ToList());
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

	Color ColorForType(BoardColorType type) {
		switch(type) {
			case BoardColorType.moveRangeHighlight:
				return moveRangeHighlightColor;
			case BoardColorType.targetRangeHighlight:
				return targetRangeHighlightColor;
			case BoardColorType.targetAreaHighlight:
				return targetAreaHighlightColor;
			case BoardColorType.viewingRangeHighlight:
				return viewingRangeHighlightColor;
			case BoardColorType.viewingRangeEdgeHighlight:
				return viewingRangeEdgeHighlightColor;
			default:
				return defaultTileColor;
		}
	}
	#endregion
}

public enum BoardColorType {
	moveRangeHighlight, targetRangeHighlight, targetAreaHighlight, viewingRangeHighlight, viewingRangeEdgeHighlight, defaultTile
}