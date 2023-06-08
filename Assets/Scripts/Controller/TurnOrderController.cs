using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TurnOrderController : MonoBehaviour 
{
	#region Constants
	const int turnActivation = 100;
	const int turnCost = 50;
	const int moveCost = 30;
	const int actionCost = 20;
	const int maxEntryCount = 5;

	const string ShowKey = "Show";
	const string HideKey = "Hide";
	const string EntryPoolKey = "TurnOrderEntryKey";
	#endregion

	#region Notifications
	public const string RoundBeganNotification = "TurnOrderController.roundBegan";
	public const string TurnCheckNotification = "TurnOrderController.turnCheck";
	public const string TurnBeganNotification = "TurnOrderController.TurnBeganNotification";
	public const string TurnCompletedNotification = "TurnOrderController.turnCompleted";
	public const string RoundEndedNotification = "TurnOrderController.roundEnded";
	#endregion

	#region Public

	[SerializeField] public GameObject entryPrefab;
    [SerializeField] public Panel panel;

	public List<Text> textEntries = new List<Text>(maxEntryCount);

	private void Awake() {
		GameObjectPoolController.AddEntry(EntryPoolKey, entryPrefab, maxEntryCount, int.MaxValue);
    }

	public IEnumerator Round() {
		while (true) {
			this.PostNotification(RoundBeganNotification);

			for (int i = 0; i < bc.units.Count; ++i) {
				Stats s = bc.units[i].GetComponent<Stats>();
				s[StatTypes.CTR] += s[StatTypes.SPD];
			}

			for (int i = bc.units.Count - 1; i >= 0; --i) {
				UpdateTurnOrderUI();
				
				Unit unit = bc.units[i];
				if (CanTakeTurn(unit)) {
					bc.turn.Change(unit);
					unit.PostNotification(TurnBeganNotification);

					yield return unit;

					int cost = turnCost;
					if (bc.turn.hasUnitMoved)
						cost += moveCost;
					if (bc.turn.hasUnitActed)
						cost += actionCost;

					Stats s = unit.GetComponent<Stats>();
					s.SetValue(StatTypes.CTR, s[StatTypes.CTR] - cost, false);

					unit.PostNotification(TurnCompletedNotification);
				}
			}
			
			this.PostNotification(RoundEndedNotification);
		}
	}
	#endregion

	#region Private

	BattleController bc { get { return GetComponentInParent<BattleController>(); } }

	List<Unit> unitsSortedByTurnOrder { get {
		var units = new List<Unit>( bc.units );
		units.Sort( (a,b) => GetCounter(a).CompareTo(GetCounter(b)));
		return units;
	} }

	// Turn Order UI
	void UpdateTurnOrderUI() {
		Clear();

		var units = new List<Unit>(unitsSortedByTurnOrder);
		units.Reverse();
		for (int i = 0; i < units.Count; ++i) {
			if (i > maxEntryCount) return;

			Unit unit = units[i];
			Stats s = unit.GetComponent<Stats>();

			Text entry = Dequeue();
			entry.text = string.Format("{0}: {1}", unit.name, s[StatTypes.CTR].ToString());
			entry.fontStyle = i == 0 ? FontStyle.Bold : entry.fontStyle;
			textEntries.Add(entry);
		}

		TogglePos(ShowKey);
	}

	Text Dequeue() {
		Poolable p = GameObjectPoolController.Dequeue(EntryPoolKey);
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
	// Conditional Logic

	bool CanTakeTurn(Unit target) {
		BaseAdjustment adj = new BaseAdjustment( GetCounter(target) >= turnActivation);
		target.PostNotification( TurnCheckNotification, adj);
		return adj.toggle;
	}

	int GetCounter(Unit target) {
		int counter = target.GetComponent<Stats>()[StatTypes.CTR];
		return counter;
	}

	#endregion
}