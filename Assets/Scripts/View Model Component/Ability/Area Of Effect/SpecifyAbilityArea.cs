using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpecifyAbilityArea : AbilityArea 
{
	public int horizontal;
	public int vertical;
	public bool doesPassThroughWalls;
	Tile tile;

	public override List<Tile> GetTilesInArea (Board board, Point pos)
	{
		tile = board.GetTile(pos);
		return board.Search(tile, ExpandSearch);
	}

	bool ExpandSearch (Tile from, Tile to)
	{
		// Skip if walls are blocking the way
		if (!doesPassThroughWalls && Tile.DoesWallSeparateTiles(from, to))
			return false;

		return (from.distance + 1) <= horizontal && Mathf.Abs(to.height - tile.height) <= vertical;
	}
}