using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ConstantAbilityRange : AbilityRange 
{
	public bool doesPassThroughWalls;

	public override List<Tile> GetTilesInRange (Board board)
	{
		List<Tile> tiles = board.Search(unit.tile, delegate (Tile from, Tile to) {
			return ExpandSearch(board, from, to);
		});
		return tiles;
	}
	
	bool ExpandSearch (Board board, Tile from, Tile to)
	{
		if (!doesPassThroughWalls && board.WallImpedingMissile(unit.tile, to.pos) != null)
			return false;

		return (from.distance + 1) <= horizontal && Mathf.Abs(to.height - unit.tile.height) <= vertical;
	}
}