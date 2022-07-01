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

		bool ValidateTile(Tile currentTile, Tile nextTile)
		{
			if (currentTile == null || nextTile == null)
				return false;

			if (!validatedTiles.Contains(currentTile) && currentTile != unit.tile)
				return false;

			if (Tile.DoesWallSeparateTiles(currentTile, nextTile))
				return false;

			return Mathf.Abs(nextTile.height - unit.tile.height) <= viewingRange.y;
		}

		Point pos = unit.tile.pos;
		int dir = (unit.dir == Directions.North || unit.dir == Directions.East) ? 1 : -1;
		Tile fromTile = unit.tile;

		Tile NewTile(int medial, int lateral, bool isAdditive)
		{
			Point point;
			if (unit.dir == Directions.North || unit.dir == Directions.South)
			{
				if (isAdditive)
					point = new Point(pos.x + lateral, pos.y + (medial * dir));
				else
					point = new Point(pos.x - lateral, pos.y + (medial * dir));
			}
			else
			{
				if (isAdditive)
					point = new Point(pos.x + (medial * dir), pos.y + lateral);
				else
					point = new Point(pos.x + (medial * dir), pos.y - lateral);
			}
			return board.GetTile(point);
		}

		int wing1 = 0;
		int wing2 = 0;
		for (int medial = 0; medial <= viewingRange.x; ++medial)
		{
			Point primary;
			if (unit.dir == Directions.North || unit.dir == Directions.South)
				primary = new Point(pos.x, pos.y + (medial * dir));
			else
				primary = new Point(pos.x + (medial * dir), pos.y);
			Tile primaryTile = board.GetTile(primary);
			if (ValidateTile(fromTile, primaryTile) && medial > 0)
			{
				validatedTiles.Add(primaryTile);
				primaryTile.isBeingPerceived = true;
			}
			fromTile = primaryTile;

			// Go one way from the center
			for (int lateral = 1; lateral <= wing1; ++lateral)
			{
				Tile nextTile = NewTile(medial, lateral, true);
				if (ValidateTile(fromTile, nextTile))
				{
					validatedTiles.Add(nextTile);
					nextTile.isBeingPerceived = true;
				}

				fromTile = nextTile;
			}
			++wing1;
			fromTile = primaryTile;

			// Then the other way
			for (int lateral = 1; lateral <= wing2; ++lateral)
			{
				Tile nextTile = NewTile(medial, lateral, false);
				if (ValidateTile(fromTile, nextTile))
				{
					validatedTiles.Add(nextTile);
					nextTile.isBeingPerceived = true;
				}

				fromTile = nextTile;
			}
			++wing2;
			fromTile = primaryTile;
		}

		return validatedTiles;
	}
}