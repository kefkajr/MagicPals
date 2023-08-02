using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class AwarenessController : MonoBehaviour {

	public const string NotficationEscape = "NotficationEscape";

    protected BattleController battleController;
	protected Board board { get { return battleController.board; } }
    public Dictionary<Unit, Dictionary<Unit, Awareness>> awarenessMap = new Dictionary<Unit, Dictionary<Unit, Awareness>>();
	[SerializeField] public GameObject awarenessLinePrefab;
	[SerializeField] public Material awarenessLineMaterialSeen;
	[SerializeField] public Material awarenessLineMaterialMayHaveSeen;
	[SerializeField] public Material awarenessLineMaterialLost;

	[SerializeField] public GameObject entryPrefab;
    [SerializeField] public Panel panel;
	const int maxTextEntryCount = 12;
	public List<Text> textEntries = new List<Text>(maxTextEntryCount);
	Dictionary<Awareness, AwarenessLine> awarenessLines = new Dictionary<Awareness, AwarenessLine>();
	List<Tile> highlightedViewingRangeTiles = new List<Tile>();
	private const string AwarenessLinePoolKey = "AwarenessController.Line";

	const string ShowKey = "Show";
	const string HideKey = "Hide";
	private const string AwarenessEntryPoolKey = "AwarenessEntryKey";
	bool doesEveryoneSeeEveryone;

	protected void AddListeners() {

	}

	protected void RemoveListeners() {

	}

	protected void OnDestroy() {
		RemoveListeners();
	}

	public void Setup(BattleController bc) {
		battleController = bc;
		AddListeners();
		doesEveryoneSeeEveryone = GameConfig.Main.MakeAllUnitsSeeEachOther;

		InitializeAwarenessMap();
		GameObjectPoolController.AddEntry(AwarenessLinePoolKey, awarenessLinePrefab, awarenessMap.Values.Count, int.MaxValue);

		UpdateAwarenessDescriptionDisplay();
	}

	public void InitializeAwarenessMap() {
		// Allow all units to look at any other units
		foreach (Unit perceivingUnit in battleController.units) {
			foreach (Unit perceivedUnit in battleController.units) {
				if (perceivingUnit != perceivedUnit) {

					var newAwareness = new Awareness(
						perceivingUnit.perception,
						perceivedUnit.stealth,
						perceivedUnit.tile.pos,
						doesEveryoneSeeEveryone ? AwarenessType.Seen : AwarenessType.Unaware);

					if (awarenessMap.ContainsKey(perceivingUnit)) {
						awarenessMap[perceivingUnit].Add(perceivedUnit, newAwareness);
					} else {
						awarenessMap.Add(perceivingUnit, new Dictionary<Unit, Awareness> { [perceivedUnit] = newAwareness });
					}
				}
			}

			Look(perceivingUnit);
		}
    }

	private void Awake() {
		GameObjectPoolController.AddEntry(AwarenessEntryPoolKey, entryPrefab, maxTextEntryCount, int.MaxValue);
    }

	public List<Awareness> Look(Unit unit) {
		Perception perception = unit.perception;
		List<Awareness> updatedAwarenesses = new List<Awareness>();
		Dictionary<Tile, AwarenessType> tilesInRange = GetTilesInVisibleRange(unit);
		List<AwarenessType> visibleAwarenessTypes = new List<AwarenessType> { AwarenessType.MayHaveSeen, AwarenessType.Seen };

		// first check MayHaveSeen range, then Seen range.
		foreach (AwarenessType type in visibleAwarenessTypes) {
			List<Tile> tilesByType = tilesInRange.Where(t => t.Value == type).Select(t => t.Key).ToList();
			List<GameObject> tileOccupants = tilesByType.Select(t => t.occupant).Where(o => o != null).ToList();
			List<Unit> unitsInRange = tileOccupants.Select(o => o.GetComponent<Unit>()).Where(u => u != null).ToList();
			List<Stealth> stealthsInRange = unitsInRange.Select(unit => unit.GetComponent<Stealth>()).ToList();
			foreach (Stealth stealth in stealthsInRange) {
				// If the unit is not invisible
				if (!stealth.isInvisible) {
					// Find existing awareness and update it with the perceived unit's existing location
					Awareness awareness = awarenessMap[perception.unit][stealth.unit];
					if (UpdateAwareness(awareness, type, stealth.unit.tile.pos)) {
						updatedAwarenesses.Add(awareness);
					}
				}
			}
		}

		return updatedAwarenesses;
	}

	public Dictionary<Tile, AwarenessType> GetTilesInVisibleRange(Unit unit) {
		Perception perception = unit.perception;
		Vector2 viewingRange = perception.viewingRange;
		Dictionary<Tile, AwarenessType> validatedTiles = new Dictionary<Tile, AwarenessType>();

		bool IsValidTile(Tile fromTile, Tile toTile) {
			if (fromTile == null || toTile == null)
				return false;

			List<Tile> knownTiles = validatedTiles.Keys.ToList();
			if (!knownTiles.Contains(fromTile) && fromTile != unit.tile)
				return false;

			if (board.WallSeparatingTiles(fromTile, toTile) != null)
				return false;

			return Mathf.Abs(toTile.height - unit.tile.height) <= viewingRange.y;
		}

		int dir = (unit.dir == Direction.North || unit.dir == Direction.East) ? 1 : -1;

		// Draw a line from the unit to each of the furthest tiles from their viewing range
		for (int sightline = (int)-viewingRange.x; sightline <= viewingRange.x; sightline++) {

			Point pos = unit.tile.pos;
			Point end;
			if (unit.dir == Direction.North || unit.dir == Direction.South)
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
			for (; ; ) {
				Tile tile = board.GetTile(pos);
				if (tile != null) {
					if (fromTile != null) {
						if (IsValidTile(fromTile, tile)) {
							tile.isBeingPerceived = true;
							bool isTileAtEdgeOfVisibleRange = sightline == (int)-viewingRange.x ||
								sightline == (int)viewingRange.x ||
								pos == end;

							// Make sure the edge tiles are always associated with the MayHaveSeen type,
							// and that they aren't overwritten with the Seen type.
							AwarenessType awarenessType = isTileAtEdgeOfVisibleRange ? AwarenessType.MayHaveSeen : AwarenessType.Seen;
							if (validatedTiles.ContainsKey(tile)) {
								awarenessType = validatedTiles[tile] == AwarenessType.MayHaveSeen ? validatedTiles[tile] : awarenessType;
							}
							validatedTiles[tile] = awarenessType;
						} else {
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

	public List<Awareness> Listen(Unit unit, Point pointOfNoise, List<Tile> noisyTiles, Stealth noisyStealth) {
		Perception perception = unit.GetComponent<Perception>();
		List<Awareness> updatedAwarenesses = new List<Awareness>();

		List<Tile> tilesInRange = board.Search(unit.tile, delegate (Tile from, Tile to) {
			// Height isn't being handled right now for noise perception.
			return (from.distance + 1) <= perception.hearingRange;
		});

		List<Tile> intersection = noisyTiles.Intersect(tilesInRange).ToList();
		if (intersection.Count > 0) {
			Awareness awareness = awarenessMap[perception.unit][noisyStealth.unit];
			if (UpdateAwareness(awareness, AwarenessType.MayHaveHeard, pointOfNoise)) {
				updatedAwarenesses.Add(awareness);
			}
		}
		return updatedAwarenesses;
	}

	public bool UpdateAwareness(Awareness awareness, AwarenessType type, Point pointOfInterest) {
		bool awarenessDidChange = awareness.Update(type, pointOfInterest);
		if (awarenessDidChange) {
			switch (type) {
				case AwarenessType.Unaware:
				case AwarenessType.LostTrack:
					battleController.uiController.DisplayFlyawayText(awareness.perception.unit, "...");
					break;
				case AwarenessType.MayHaveHeard:
				case AwarenessType.MayHaveSeen:
					battleController.uiController.DisplayFlyawayText(awareness.perception.unit, "?");
					break;
				case AwarenessType.Seen:
					battleController.uiController.DisplayFlyawayText(awareness.perception.unit, "!");
					break;
				default:
					break;
			}
			Console.Main.Log(awareness.ToString());
			UpdateAwarenessDescriptionDisplay();
			DisplayAwarenessLines(awareness.perception.unit, true);
		}
		return awarenessDidChange;
	}

	public bool IsAwareOfUnit(Unit perceivingUnit, Unit perceivedUnit, AwarenessType[] types) {
		if (perceivingUnit == perceivedUnit)
			return false;

		Awareness relevantAwareness = awarenessMap[perceivingUnit][perceivedUnit];
		return types.Contains(relevantAwareness.type);
	}

	public List<Awareness> TopAwarenesses(Unit perceivingUnit) {
		if (awarenessMap.Count == 0) return new List<Awareness>();
		List<Awareness> relevantAwarenesses = awarenessMap[perceivingUnit].Select(kv => kv.Value).ToList();
		List<Awareness> activeAwarenesses = relevantAwarenesses.Where(a => a.type != AwarenessType.Unaware).ToList();
		List<Awareness> orderedAwarenesses = activeAwarenesses.OrderByDescending(a => (int)a.type).ToList();
		return orderedAwarenesses;
	}

	public void InitiateEmergencyTurn(Unit unit) {
		Console.Main.Log(string.Format("{0} was spotted! Receiving 1000 CTR", unit.name));
		Stats s = unit.GetComponent<Stats>();
		s.SetValue(StatTypes.CTR, 1000, false);
		battleController.ChangeState<SelectUnitState>();
	}

	public void HandleUnitEscape(Unit unit) {
		unit.stealth.isInvisible = true;
		unit.tile.occupant = null;

		foreach (Unit perceivingUnit in awarenessMap.Keys) {
			if (perceivingUnit == unit) continue;
			Awareness awareness = awarenessMap[perceivingUnit][unit];
			UpdateAwareness(awareness, AwarenessType.Unaware, unit.tile.pos);
		}

		unit.transform.SetParent(GameObject.Find("Escaped Units").transform);

        battleController.escapedUnits.Add(unit);
        battleController.units.Remove(unit);

        unit.gameObject.SetActive(false);

		this.PostNotification(NotficationEscape, unit);
	}

	// Awareness Lines and Viewing Ranges

	public void DisplayAwarenessLines(Unit unit, bool shouldFadeOut = false) {
		ClearAwarenessLines();
		Alliance perceiverAlliance = unit.GetComponentInChildren<Alliance>();
		foreach (Awareness a in TopAwarenesses(unit)) {
			Alliance perceivedAlliance = a.stealth.GetComponentInChildren<Alliance>();
			bool isAlly = perceiverAlliance.IsMatch(perceivedAlliance, TargetType.Ally);
			if (isAlly) continue;

			Poolable p = GameObjectPoolController.Dequeue(AwarenessLinePoolKey);
			AwarenessLine line = p.GetComponent<AwarenessLine>();
			line.transform.SetParent(battleController.transform, false);
			line.transform.localScale = Vector3.one;
			line.gameObject.SetActive(true);
			line.SetAwareness(a, AwarenessLineMaterial(a.type));
			
			if (shouldFadeOut) {
				line.Fadeout();
			}

			awarenessLines[a] = line;
		}
	}

	public void ClearAwarenessLines() {
		foreach (AwarenessLine l in awarenessLines.Values) {
			if (l.isFading) continue;
			
			Poolable p = l.GetComponent<Poolable>();
			GameObjectPoolController.Enqueue(p);
		}
	}

	public void DisplayViewingRange(Unit unit) {
		Dictionary<Tile, AwarenessType> tilesInRange = GetTilesInVisibleRange(unit);
		List<Tile> mainRangeTiles = tilesInRange.Where(tap => tap.Value == AwarenessType.Seen).Select(tap => tap.Key).ToList();
		List<Tile> edgeRangeTiles = tilesInRange.Where(tap => tap.Value == AwarenessType.MayHaveSeen).Select(tap => tap.Key).ToList();

		battleController.board.HighlightTiles(mainRangeTiles, TileHighlightColorType.viewingRangeHighlight);
		battleController.board.HighlightTiles(edgeRangeTiles, TileHighlightColorType.viewingRangeEdgeHighlight);

		mainRangeTiles.AddRange(edgeRangeTiles);
		highlightedViewingRangeTiles = mainRangeTiles;
	}

	public void HideViewingRanges() {
		battleController.board.DeHighlightTiles(highlightedViewingRangeTiles);
	}

	public Material AwarenessLineMaterial(AwarenessType type)
	{
		switch (type)
		{
			case AwarenessType.MayHaveHeard:
			case AwarenessType.MayHaveSeen:
				return awarenessLineMaterialMayHaveSeen;
			case AwarenessType.Seen:
				return awarenessLineMaterialSeen;
			default: // Unaware:
				return awarenessLineMaterialLost;
		};
	}

	// Awareness Description Display

	void UpdateAwarenessDescriptionDisplay() {
		Clear();

		foreach (Dictionary<Unit, Awareness> awarenessDict in awarenessMap.Values.ToList() ) {
			foreach (Awareness awareness in awarenessDict.Values.ToList() ) {
				if (textEntries.Count >= maxTextEntryCount) return;

				Text entry = Dequeue();
				entry.text = awareness.ToString();
				switch (awareness.type) {
				case AwarenessType.Unaware:
					entry.color = Color.white;
					break;
				case AwarenessType.LostTrack:
					entry.color = Color.gray;
					break;
				case AwarenessType.MayHaveHeard:
				case AwarenessType.MayHaveSeen:
					entry.color = Color.yellow;
					break;
				case AwarenessType.Seen:
					entry.color = Color.red;
					break;
				default:
					break;
			}
				textEntries.Add(entry);
			}
		}

		TogglePos(ShowKey);
	}

	Text Dequeue() {
		Poolable p = GameObjectPoolController.Dequeue(AwarenessEntryPoolKey);
		Text entry = p.GetComponent<Text>();
		entry.transform.SetParent(panel.transform, false);
		entry.transform.localScale = Vector3.one;
		entry.gameObject.SetActive(true);
		return entry;
	}

	void Enqueue(Text entry) {
		Poolable p = entry.GetComponent<Poolable>();
		GameObjectPoolController.Enqueue(p);
	}

	void Clear() {
		for (int i = textEntries.Count - 1; i >= 0; --i)
			Enqueue(textEntries[i]);
		textEntries.Clear();
	}

	Tweener TogglePos(string pos) {
		Tweener t = panel.SetPosition(pos, true);
		t.duration = 0.5f;
		t.equation = EasingEquations.EaseOutQuad;
		return t;
	}

	void OnEnable() {
		this.AddObserver(AwarenessLevelDecay, TurnOrderController.TurnCompletedNotification);
	}

	void OnDisable() {
		this.RemoveObserver(AwarenessLevelDecay, TurnOrderController.TurnCompletedNotification);
	}

	void AwarenessLevelDecay(object sender, object args) {
		Console.Main.Log("Awareness Decay");
		foreach (Unit perceivingUnit in awarenessMap.Keys) {
			foreach (Unit perceivedUnit in awarenessMap[perceivingUnit].Keys) {
				awarenessMap[perceivingUnit][perceivedUnit].Decay();
			}
		}

		UpdateAwarenessDescriptionDisplay();
	}


	void OnDrawGizmos() {
		// foreach (Unit perceivingUnit in awarenessMap.Keys)
		// {
		// 	foreach (Unit perceivedUnit in awarenessMap[perceivingUnit].Keys)
		// 	{
		// 		Awareness awareness = awarenessMap[perceivingUnit][perceivedUnit];
		// 		Color color = awareness.type.GizmoColor();
		// 		color.a = 0.5f;
		// 		Gizmos.color = color;
		// 		Vector3 perceiverPosition = GameObject.Find(string.Format("Units/{0}/Jumper/Sphere/Sphere", awareness.perception.unit.name)).transform.position + Vector3.up * 0.5f;
		// 		Vector3 perceivedPosition = GameObject.Find(string.Format("Units/{0}/Jumper/Sphere/Sphere 1", awareness.stealth.unit.name)).transform.position + Vector3.up * 0.5f;
		// 		for (float i = 0.1f; i < 1f; i += 0.1f)
		// 		{
		// 			Gizmos.DrawCube(Vector3.Lerp(perceiverPosition, perceivedPosition, i), new Vector3(0.1f, 0.1f, 0.1f));
		// 		}
		// 	}
		// }
	}
}
