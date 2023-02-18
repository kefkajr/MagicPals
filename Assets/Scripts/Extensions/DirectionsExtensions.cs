using UnityEngine;
using System.Collections.Generic;

public static class DirectionsExtensions
{
	public static List<Direction> GetDirections (this Tile t1, Tile t2)
	{
		List<Direction> directions = new List<Direction>();

		if (t1.pos.y < t2.pos.y)
			directions.Add(Direction.North);
		if (t1.pos.x < t2.pos.x)
			directions.Add(Direction.East);
		if (t1.pos.y > t2.pos.y)
			directions.Add(Direction.South);
		if (t1.pos.x > t2.pos.x)
			directions.Add(Direction.West);
		
		return directions;
	}

	public static Vector3 ToEuler (this Direction d)
	{
		return new Vector3(0, (int)d * 90, 0);
	}

	public static Direction GetDirection (this Point p)
	{
		if (p.y > 0)
			return Direction.North;
		if (p.x > 0)
			return Direction.East;
		if (p.y < 0)
			return Direction.South;
		return Direction.West;
	}

	public static Direction GetOpposite(this Direction dir)
	{
		switch (dir)
		{
			case Direction.North:
				return Direction.South;
			case Direction.East:
				return Direction.West;
			case Direction.South:
				return Direction.North;
			default: // Direction.West:
				return Direction.East;
		};
	}

	public static Point GetNormal (this Direction dir)
	{
		switch (dir)
		{
		case Direction.North:
			return new Point(0, 1);
		case Direction.East:
			return new Point(1, 0);
		case Direction.South:
			return new Point(0, -1);
		default: // Direction.West:
			return new Point(-1, 0);
		}
	}
}