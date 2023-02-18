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
		return board.Search(tile, delegate (Tile from, Tile to) {
			return ExpandSearch(board, from, to);
		});
	}

	bool ExpandSearch (Board board, Tile from, Tile to)
	{
		// Skip if walls are blocking the way
		if (!doesPassThroughWalls && board.WallSeparatingTiles(from, to) != null)
			return false;

		return (from.distance + 1) <= horizontal && Mathf.Abs(to.height - tile.height) <= vertical;
	}
}