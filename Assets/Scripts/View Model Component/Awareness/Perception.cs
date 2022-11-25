using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class Perception : MonoBehaviour
{
	public Vector2 viewingRange = Vector2.zero; // cone, length and diameter
	public float hearingRange = 0f; // radius of a circle
	public HashSet<Awareness> awarenesses = new HashSet<Awareness>(); // list of awarenesses, known stealths and their types

	public Unit unit { get { return GetComponentInParent<Unit>(); } }

	public List<Awareness> Look(Board board)
	{
		List<Awareness> newAwarenesses = new List<Awareness>();
		HashSet<TileAwarenessTypePair> tilesInRange = GetTilesInVisibleRange(board);
		List<AwarenessType> visibleAwarenessTypes = new List<AwarenessType> { AwarenessType.MayHaveSeen, AwarenessType.Seen };

		foreach (AwarenessType type in visibleAwarenessTypes)
        {
			List<Tile> tilesByType = tilesInRange.Where(t => t.awarenessType == type).Select(t => t.tile).ToList();
			List<GameObject> tileOccupants = tilesByType.Select(t => t.occupant).Where(o => o != null).ToList();
			List<Unit> unitsInRange = tileOccupants.Select(o => o.GetComponent<Unit>()).Where(u => u != null).ToList();
			List<Stealth> stealthsInRange = unitsInRange.Select(unit => unit.GetComponent<Stealth>()).ToList();
			foreach (Stealth stealth in stealthsInRange)
			{
				// If the unit is not invisible
				if (!stealth.isInvisible)
				{
					// Construct an awareness based on the visible awareness type
					Awareness potentialNewAwareness = new Awareness(this, stealth, stealth.unit.tile.pos, type);
					if (!awarenesses.Contains(potentialNewAwareness))
                    {
						// Add the brand new awareness both to the NEW awarenesses to be reported
                        // AND to the awarenesses that should be tracked by this perception
						newAwarenesses.Add(potentialNewAwareness);
						awarenesses.Add(potentialNewAwareness);
						Console.Main.Log(potentialNewAwareness.ToString());
					} else
                    {
						// If there is an update to an awareness based on a change in the visible range,
						// add it to the new awarenesses to be reported
						Awareness knownAwareness = awarenesses.Where(a => a.stealth == stealth).First();
						if (knownAwareness.Update(type))
						{
							newAwarenesses.Add(potentialNewAwareness);
							Console.Main.Log(potentialNewAwareness.ToString());
						}
					}
				}
			}
		}		

		return newAwarenesses;
	}

	public HashSet<TileAwarenessTypePair> GetTilesInVisibleRange(Board board)
	{
		HashSet<TileAwarenessTypePair> validatedTiles = new HashSet<TileAwarenessTypePair>();

		bool IsValidTile(Tile fromTile, Tile toTile)
        {
            if (fromTile == null || toTile == null)
                return false;

			List<Tile> knownTiles = validatedTiles.Select(t => t.tile).ToList();
			if (!knownTiles.Contains(fromTile) && fromTile != unit.tile)
                return false;

            if (Tile.DoesWallSeparateTiles(fromTile, toTile))
                return false;

			// This is a separate wall check necessary for tiles diagonal to each other
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
							bool isTileAtEdgeOfVisibleRange = sightline == (int)-viewingRange.x ||
								sightline == (int)viewingRange.x ||
								pos == end;
							tile.gizmoColor = isTileAtEdgeOfVisibleRange ? Color.yellow : Color.red;
							validatedTiles.Add(new TileAwarenessTypePair(tile, isTileAtEdgeOfVisibleRange ? AwarenessType.MayHaveSeen : AwarenessType.Seen));
						} else
                        {
							break;
                        }
					}
					fromTile = tile;
                }
                if (pos == end) break;
				e2 = err;
				if (e2 > -dx) { err -= dy; pos.x += sx; }
				if (e2 < dy) { err += dx; pos.y += sy; }
			}

		}

		return validatedTiles;
	}

	public List<Awareness> Listen(Board board, Point pointOfNoise, List<Tile> noisyTiles, Stealth noisyStealth)
    {
		List<Awareness> newAwarenesses = new List<Awareness>();
		List<Tile> tilesInRange = board.Search(unit.tile, HearingExpandSearch);
		List<Tile> intersection = noisyTiles.Intersect(tilesInRange).ToList();
		if (intersection.Count > 0)
        {
			// Construct an awareness based on the heard awareness type
			Awareness potentialNewAwareness = new Awareness(this, noisyStealth, pointOfNoise, AwarenessType.MayHaveHeard);
			if (!awarenesses.Contains(potentialNewAwareness))
			{
				// Add the brand new awareness both to the NEW awarenesses to be reported
				// AND to the awarenesses that should be tracked by this perception
				newAwarenesses.Add(potentialNewAwareness);
				awarenesses.Add(potentialNewAwareness);
				Console.Main.Log(potentialNewAwareness.ToString());
			}
			else
			{
				// If there is an update to an awareness based on a change in the hearing range,
				// add it to the new awarenesses to be reported
				Awareness knownAwareness = awarenesses.Where(a => a.stealth == noisyStealth).First();
				if (knownAwareness.Update(AwarenessType.MayHaveHeard))
				{
					newAwarenesses.Add(potentialNewAwareness);
					Console.Main.Log(potentialNewAwareness.ToString());
				}
			}
		}
		return newAwarenesses;
	}

	bool HearingExpandSearch(Tile from, Tile to)
	{
		// Height isn't being handled right now for noise perception.
		return (from.distance + 1) <= hearingRange;
	}

	public bool IsAwareOfUnit(Unit unit, AwarenessType type)
    {
		Stealth stealth = unit.GetComponent<Stealth>();
		List<Awareness> relevantAwarenesses = awarenesses.Where(a => a.stealth == stealth && a.type == type).ToList();
		return relevantAwarenesses.Count > 0;
    }

	public List<Awareness> TopAwarenesses ()
    {
		List<Awareness> relevantAwarenesses = awarenesses.OrderByDescending(a => (int)a.type).ToList();
		return relevantAwarenesses;
	}

	void OnEnable()
	{
		this.AddObserver(AwarenessLevelDecay, TurnOrderController.TurnCompletedNotification);
	}

	void OnDisable()
	{
		this.RemoveObserver(AwarenessLevelDecay, TurnOrderController.TurnCompletedNotification);
	}

	void AwarenessLevelDecay(object sender, object args)
	{
		HashSet<Awareness> expiredAwarenesses = new HashSet<Awareness>();
		foreach (Awareness awareness in awarenesses)
        {
			awareness.Decay();
			if (awareness.isExpired)
			{
				expiredAwarenesses.Add(awareness);
				Console.Main.Log(string.Format("{0} does not know where {1} is", unit.name, awareness.stealth.unit.name));
				awareness.perception = null; // Don't know if this is necessary, but it seems safe.
			}
        }
		awarenesses.ExceptWith(expiredAwarenesses);
	}

	void OnDrawGizmos()
	{
		foreach (Awareness awareness in awarenesses)
		{
			Color color = awareness.type.GizmoColor();
			color.a = 0.5f;
			Gizmos.color = color;
			Vector3 perceiverPosition = GameObject.Find(string.Format("Units/{0}/Jumper/Sphere/Sphere", unit.name)).transform.position + Vector3.up * 0.5f;
			Vector3 perceivedPosition = GameObject.Find(string.Format("Units/{0}/Jumper/Sphere/Sphere 1", awareness.stealth.unit.name)).transform.position + Vector3.up * 0.5f;
			for(float i = 0.1f; i < 1f; i += 0.1f)
            {
				Gizmos.DrawCube(Vector3.Lerp(perceiverPosition, perceivedPosition, i), new Vector3(0.1f, 0.1f, 0.1f));
			}
		}
	}
}

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