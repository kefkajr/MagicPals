using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class AwarenessController : MonoBehaviour
{
    protected BattleController battleController;
	protected Board board { get { return battleController.board; } }

    public Dictionary<Unit, Dictionary<Unit, Awareness>> awarenessMap = new Dictionary<Unit, Dictionary<Unit, Awareness>>();

    protected virtual void Awake()
    {
        battleController = GetComponent<BattleController>();
        AddListeners();
    }

	protected void AddListeners()
	{

	}

	protected void RemoveListeners()
	{

	}

	protected void OnDestroy()
	{
		RemoveListeners();
	}

	public void InitializeAwarenessMap()
    {
		// Allow all units to look at any other units
		foreach (Unit perceivingUnit in battleController.units)
		{
			foreach (Unit perceivedUnit in battleController.units)
			{
				if (perceivingUnit != perceivedUnit)
				{

					var newAwareness = new Awareness(perceivingUnit.perception, perceivedUnit.stealth, perceivedUnit.tile.pos, AwarenessType.Unaware);

					if (awarenessMap.ContainsKey(perceivingUnit))
					{
						awarenessMap[perceivingUnit].Add(perceivedUnit, newAwareness);
					}
					else
					{
						awarenessMap.Add(perceivingUnit, new Dictionary<Unit, Awareness> { [perceivedUnit] = newAwareness });
					}
				}
			}

			Look(perceivingUnit);
		}
    }

	public List<Awareness> Look(Unit unit)
	{
		Perception perception = unit.GetComponent<Perception>();
		List<Awareness> updatedAwarenesses = new List<Awareness>();
		HashSet<TileAwarenessTypePair> tilesInRange = GetTilesInVisibleRange(unit);
		List<AwarenessType> visibleAwarenessTypes = new List<AwarenessType> { AwarenessType.MayHaveSeen, AwarenessType.Seen };

		// first check MayHaveSeen range, then Seen range.
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
					// Find existing awareness and update it with the perceived unit's existing location
					Awareness awareness = awarenessMap[perception.unit][stealth.unit];
					if (awareness.Update(type, stealth.unit.tile.pos))
                    {
						updatedAwarenesses.Add(awareness);
						Console.Main.Log(awareness.ToString());
					}
				}
			}
		}

		return updatedAwarenesses;
	}

	public HashSet<TileAwarenessTypePair> GetTilesInVisibleRange(Unit unit)
	{
		Perception perception = unit.GetComponent<Perception>();
		Vector2 viewingRange = perception.viewingRange;
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
		for (int sightline = (int)-viewingRange.x; sightline < viewingRange.x; sightline++)
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
					if (fromTile != null)
					{
						if (IsValidTile(fromTile, tile))
						{
							tile.isBeingPerceived = true;
							tile.gizmoAlpha = 1;
							bool isTileAtEdgeOfVisibleRange = sightline == (int)-viewingRange.x ||
								sightline == (int)viewingRange.x ||
								pos == end;
							tile.gizmoColor = isTileAtEdgeOfVisibleRange ? Color.yellow : Color.red;
							validatedTiles.Add(new TileAwarenessTypePair(tile, isTileAtEdgeOfVisibleRange ? AwarenessType.MayHaveSeen : AwarenessType.Seen));
						}
						else
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

	public List<Awareness> Listen(Unit unit, Point pointOfNoise, List<Tile> noisyTiles, Stealth noisyStealth)
	{
		Perception perception = unit.GetComponent<Perception>();
		List<Awareness> updatedAwarenesses = new List<Awareness>();

		List<Tile> tilesInRange = board.Search(unit.tile, delegate (Tile from, Tile to) {
			// Height isn't being handled right now for noise perception.
			return (from.distance + 1) <= perception.hearingRange;
		});

		List<Tile> intersection = noisyTiles.Intersect(tilesInRange).ToList();
		if (intersection.Count > 0)
		{
			Awareness awareness = awarenessMap[perception.unit][noisyStealth.unit];
			if (awareness.Update(AwarenessType.MayHaveHeard, pointOfNoise))
			{
				updatedAwarenesses.Add(awareness);
				Console.Main.Log(awareness.ToString());
			}
		}
		return updatedAwarenesses;
	}

	public bool IsAwareOfUnit(Unit perceivingUnit, Unit perceivedUnit, AwarenessType type)
	{
		if (perceivingUnit == perceivedUnit)
			return false;

		Awareness relevantAwareness = awarenessMap[perceivingUnit][perceivedUnit];
		return relevantAwareness.type == type;
	}

	public List<Awareness> TopAwarenesses(Unit perceivingUnit)
	{
		List<Awareness> relevantAwarenesses = awarenessMap[perceivingUnit].Select(kv => kv.Value).ToList();
		List<Awareness> activeAwarenesses = relevantAwarenesses.Where(a => a.type != AwarenessType.Unaware).ToList();
		List<Awareness> orderedAwarenesses = activeAwarenesses.OrderByDescending(a => (int)a.type).ToList();
		return orderedAwarenesses;
	}

	public void InitiateEmergencyTurn(Unit unit)
    {
		Console.Main.Log(string.Format("{0} was spotted! Receivng 1000 CTR", unit.name));
		Stats s = unit.GetComponent<Stats>();
		s.SetValue(StatTypes.CTR, 1000, false);
		battleController.ChangeState<SelectUnitState>();
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
		foreach (Unit perceivingUnit in awarenessMap.Keys)
        {
			foreach (Unit perceivedUnit in awarenessMap[perceivingUnit].Keys)
			{
				awarenessMap[perceivingUnit][perceivedUnit].Decay();
			}
		}
	}

	void OnDrawGizmos()
	{
		foreach (Unit perceivingUnit in awarenessMap.Keys)
		{
			foreach (Unit perceivedUnit in awarenessMap[perceivingUnit].Keys)
			{
				Awareness awareness = awarenessMap[perceivingUnit][perceivedUnit];
				Color color = awareness.type.GizmoColor();
				color.a = 0.5f;
				Gizmos.color = color;
				Vector3 perceiverPosition = GameObject.Find(string.Format("Units/{0}/Jumper/Sphere/Sphere", awareness.perception.unit.name)).transform.position + Vector3.up * 0.5f;
				Vector3 perceivedPosition = GameObject.Find(string.Format("Units/{0}/Jumper/Sphere/Sphere 1", awareness.stealth.unit.name)).transform.position + Vector3.up * 0.5f;
				for (float i = 0.1f; i < 1f; i += 0.1f)
				{
					Gizmos.DrawCube(Vector3.Lerp(perceiverPosition, perceivedPosition, i), new Vector3(0.1f, 0.1f, 0.1f));
				}
			}
		}
	}
}
