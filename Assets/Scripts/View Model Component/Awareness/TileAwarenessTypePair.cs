using System;

public struct TileAwarenessTypePair : IEquatable<TileAwarenessTypePair>
{
	public Tile tile;
	public AwarenessType awarenessType;

	public TileAwarenessTypePair(Tile tile, AwarenessType awarenessType)
	{
		this.tile = tile;
		this.awarenessType = awarenessType;
	}

	public override bool Equals(object obj)
	{
		if (obj is TileAwarenessTypePair)
		{
			TileAwarenessTypePair t = (TileAwarenessTypePair)obj;
			return Equals(t);
		}
		return false;
	}

	public bool Equals(TileAwarenessTypePair t)
	{
		return tile == t.tile;
	}

	public override int GetHashCode()
	{
		return tile.pos.x ^ tile.pos.y;
	}

	public override string ToString()
	{
		return string.Format("({0},{1}): {2}", tile.pos.x, tile.pos.y, awarenessType.ToString());
	}
}
