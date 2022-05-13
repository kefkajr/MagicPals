using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class ConeAbilityRange : AbilityRange 
{
	public override bool directionOriented { get { return true; }}

	List<Tile> validatedTiles;

	public override List<Tile> GetTilesInRange (Board board)
	{
		validatedTiles = new List<Tile>();
		Point pos = unit.tile.pos;
		int dir = (unit.dir == Directions.North || unit.dir == Directions.East) ? 1 : -1;
		Tile fromTile = unit.tile;

		Tile NewTile(int medial, int lateral, Directions direction, bool isAdditive)
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
		for (int medial = 1; medial <= horizontal; ++medial)
		{
			Point primary;
			if (unit.dir == Directions.North || unit.dir == Directions.South)
				primary = new Point(pos.x, pos.y + (medial * dir));
			else
				primary = new Point(pos.x + (medial * dir), pos.y);
			Tile primaryTile = board.GetTile(primary);
			if (ValidateTile(board, fromTile, primaryTile))
				validatedTiles.Add(primaryTile);
			fromTile = primaryTile;

			// Go one way from the center
			for (int lateral = 1; lateral <= wing1; ++lateral)
			{
				Tile nextTile = NewTile(medial, lateral, unit.dir, true);
				if (ValidateTile(board, fromTile, nextTile))
					validatedTiles.Add(nextTile);
				else
					--wing1; // Decrease range of wing

				fromTile = nextTile;
			}
			++wing1;
			fromTile = primaryTile;

			// Then the other way
			for (int lateral = 1; lateral <= wing2; ++lateral)
			{
				Tile nextTile = NewTile(medial, lateral, unit.dir, false);
				if (ValidateTile(board, fromTile, nextTile))
					validatedTiles.Add(nextTile);
				else
					--wing2; // Decrease range of wing

				fromTile = nextTile;
			}
			++wing2;
			fromTile = primaryTile;
		}

		return validatedTiles;
	}
	
	bool ValidateTile (Board board, Tile fromTile, Tile toTile)
	{
		if (fromTile == null || toTile == null)
			return false;

		if (!validatedTiles.Contains(fromTile) && fromTile != unit.tile)
			return false;

		if (Tile.DoesWallSeparateTiles(fromTile, toTile))
			return false;

		return Mathf.Abs(toTile.height - unit.tile.height) <= vertical;
	}
}