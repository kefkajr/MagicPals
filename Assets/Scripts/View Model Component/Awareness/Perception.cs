using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Perception : MonoBehaviour
{
	public Vector2 viewingRange = Vector2.zero; // cone, length and diameter
	public float hearingRange = 0f; // radius of a circle
	public List<Stealth> perceivedStealths = new List<Stealth>(); // stealth objects whose locations are being tracked

	public Unit unit { get { return GetComponentInParent<Unit>(); } }

	public List<Stealth> Perceive(Tile tile, Board board)
	{
		List<Tile> tilesInRange = GetTilesInRange(board);
		List<GameObject> tileOccupants = tilesInRange.Select(t => t.occupant).Where(o => o != null).ToList();
		List <Unit> unitsInRange = tileOccupants.Select(o => o.GetComponent<Unit>()).Where(u => u != null).ToList();
		List<Stealth> stealthsInRange = unitsInRange.Select(unit => unit.GetComponent<Stealth>()).ToList();
		List<Stealth> newlyPerceivedStealths = new List<Stealth>();
		foreach (Stealth stealth in stealthsInRange)
        {
			if (!stealth.isInvisible)
			{
				newlyPerceivedStealths.Add(stealth);
			}
        }
		newlyPerceivedStealths = newlyPerceivedStealths.Except(perceivedStealths).ToList();

		// Add newly perceived stealth components to the full list of perceived stealth components
		foreach (Stealth stealth in newlyPerceivedStealths)
        {
			perceivedStealths.Add(stealth);
        }

		return newlyPerceivedStealths;
	}

	// Logic borrowed from the Cone ability range
	// Pivotal difference: The cone starts on the unit's tile, not on the tile in front of the unit
	public List<Tile> GetTilesInRange(Board board)
	{
		List<Tile> validatedTiles = new List<Tile>();

		bool IsValidTile(Tile fromTile, Tile toTile)
        {
            if (fromTile == null || toTile == null)
                return false;

            if (!validatedTiles.Contains(fromTile) && fromTile != unit.tile)
                return false;

            if (Tile.DoesWallSeparateTiles(fromTile, toTile))
                return false;

			// This is a separate wall check necessary for tile diagonal to each other
			bool doTilesShareAnAxis = fromTile.pos.x == toTile.pos.x || fromTile.pos.y == toTile.pos.y;

			if (!doTilesShareAnAxis)
			{
				if (fromTile.walls.ContainsKey(unit.dir) || toTile.walls.ContainsKey(unit.dir.GetOpposite()))
					return false;

				Point adjacentPointTowardUnit = toTile.pos + unit.dir.GetOpposite().GetNormal();
				Tile adjacentTileTowardUnit = board.GetTile(adjacentPointTowardUnit);
				if (adjacentTileTowardUnit != null)
				{
					Directions directionTowardFromTile = adjacentTileTowardUnit.GetDirection(fromTile);
					if (adjacentTileTowardUnit.walls.ContainsKey(unit.dir) ||
						adjacentTileTowardUnit.walls.ContainsKey(directionTowardFromTile) ||
						fromTile.walls.ContainsKey(directionTowardFromTile.GetOpposite()))
						return false;
				}
			}

			return Mathf.Abs(toTile.height - unit.tile.height) <= viewingRange.y;
        }

		int dir = (unit.dir == Directions.North || unit.dir == Directions.East) ? 1 : -1;

		// Draw a line from the unit to each of the furthest tiles from their viewing range
		for (int sightline = (int)-viewingRange.x; sightline < viewingRange.x; sightline ++)
        {	

			Point pos = unit.tile.pos;
			Point end;
			if (unit.dir == Directions.North || unit.dir == Directions.South)
                end = pos + new Point(sightline, (int)viewingRange.x * dir);
			else
				end = pos + new Point((int)viewingRange.x * dir, sightline);

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
				Tile tile = board.GetTile(pos);
				if (tile != null)
                {
					if (fromTile != null) {
						if (IsValidTile(fromTile, tile)) {
							tile.isBeingPerceived = true;
							tile.gizmoAlpha = 1;
							validatedTiles.Add(tile);
						} else
                        {
							break;
                        }
					}
					fromTile = tile;
                }
                if (pos.x == end.x && pos.y == end.y) break;
				e2 = err;
				if (e2 > -dx) { err -= dy; pos.x += sx; }
				if (e2 < dy) { err += dx; pos.y += sy; }
			}

		}

		return validatedTiles;
	}
}