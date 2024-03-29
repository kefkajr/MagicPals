﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LineAbilityRange : AbilityRange 
{
	public override bool directionOriented { get { return true; }}
	
	public override List<Tile> GetTilesInRange (Board board)
	{
		Point startPos = unit.tile.pos;
		Point endPos;
		List<Tile> retValue = new List<Tile>();
		
		switch (unit.dir)
		{
		case Direction.North:
			endPos = new Point(startPos.x, board.max.y);
			break;
		case Direction.East:
			endPos = new Point(board.max.x, startPos.y);
			break;
		case Direction.South:
			endPos = new Point(startPos.x, board.min.y);
			break;
		default: // West
			endPos = new Point(board.min.x, startPos.y);
			break;
		}

		int dist = 0;
		Point lastPos = startPos;
		Point currentPos = new Point(startPos.x, startPos.y);
		while (currentPos != endPos)
		{
			if (currentPos.x < endPos.x) currentPos.x++;
			else if (currentPos.x > endPos.x) currentPos.x--;
			
			if (currentPos.y < endPos.y) currentPos.y++;
			else if (currentPos.y > endPos.y) currentPos.y--;

			Tile startTile = board.GetTile(lastPos);
			Tile currentTile = board.GetTile(currentPos);
			if (currentTile == null || board.WallSeparatingTiles(startTile, currentTile) != null)
				break;

			if (currentTile != null && Mathf.Abs(currentTile.height - unit.tile.height) <= vertical)
				retValue.Add(currentTile);

			dist++;
			lastPos = currentPos;
			if (dist >= horizontal)
				break;
		}
		
		return retValue;
	}
}