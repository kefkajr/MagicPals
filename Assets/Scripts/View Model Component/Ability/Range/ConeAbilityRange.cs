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

		// Commit these changes, then try to combine the North/South with the East/West
		if (unit.dir == Directions.North || unit.dir == Directions.South)
		{
			int wing1 = 0;
			int wing2 = 0;
			for (int y = 1; y <= horizontal; ++y)
			{
				Point primary = new Point(pos.x, pos.y + (y * dir));
				Tile primaryTile = board.GetTile(primary);
				if (ValidateTile(board, fromTile, primaryTile))
					validatedTiles.Add(primaryTile);
				fromTile = primaryTile;

				// Go one way from the center
				for (int x = 1; x <= wing1; ++x)
				{
					Point next = new Point(pos.x + x, pos.y + (y * dir));
					Tile nextTile = board.GetTile(next);
					if (ValidateTile(board, fromTile, nextTile))
						validatedTiles.Add(nextTile);
					else
						--wing1; // Decrease range of wing

					fromTile = nextTile;
				}
				++wing1;
				fromTile = primaryTile;

				// Then the other way
				for (int x = 1; x <= wing2; ++x)
				{
					Point next = new Point(pos.x - x, pos.y + (y * dir));
					Tile nextTile = board.GetTile(next);
					if (ValidateTile(board, fromTile, nextTile))
						validatedTiles.Add(nextTile);
					else
						--wing2; // Decrease range of wing

					fromTile = nextTile;
				}
				++wing2;
				fromTile = primaryTile;
			}
		}
		else
		{
			int wing1 = 0;
			int wing2 = 0;
			for (int x = 1; x <= horizontal; ++x)
			{
				Point primary = new Point(pos.x + (x * dir), pos.y);
				Tile primaryTile = board.GetTile(primary);
				if (ValidateTile(board, fromTile, primaryTile))
					validatedTiles.Add(primaryTile);
				fromTile = primaryTile;

				// Go one way from the center
				for (int y = 1; y <= wing1; ++y)
				{
					Point next = new Point(pos.x + (x * dir), pos.y + y);
					Tile nextTile = board.GetTile(next);
					if (ValidateTile(board, fromTile, nextTile))
						validatedTiles.Add(nextTile);
					else
						--wing1;

					fromTile = nextTile;
				}
				++wing1;
				fromTile = primaryTile;

				// Then the other way
				for (int y = 1; y <= wing2; ++y)
				{
					Point next = new Point(pos.x + (x * dir), pos.y - y);
					Tile nextTile = board.GetTile(next);
					if (ValidateTile(board, fromTile, nextTile))
						validatedTiles.Add(nextTile);
					else
						--wing2;

					fromTile = nextTile;
				}
				++wing2;
				fromTile = primaryTile;
			}
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