using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public enum ActionType {
	Major, Move, Wait
}

public static class ActionTypeExention {
	public static int Value(this ActionType a)
	{
		switch (a)
		{
			case ActionType.Major:
				return 3;
			case ActionType.Move:
				return 2;
			case ActionType.Wait:
				return 2;
			default: return -1;
		}
	}
}

public class TurnOrderController : MonoBehaviour 
{
	#region Constants
	const int turnActivation = 5;
	const int turnStepAmount = 1;
	const int turnWaitOnlyPenality = 1;
	const int maxEntryCount = 5;

	const string ShowKey = "Show";
	const string HideKey = "Hide";
	const string EntryPoolKey = "TurnOrderEntryKey";
	#endregion

	#region Notifications
	public const string TurnCheckNotification = "TurnOrderController.turnCheck";
	public const string TurnBeganNotification = "TurnOrderController.TurnBeganNotification";
	public const string TurnCompletedNotification = "TurnOrderController.turnCompleted";
	#endregion

	#region Public

	[SerializeField] public GameObject entryPrefab;
    [SerializeField] public Panel panel;

	public List<Text> textEntries = new List<Text>(maxEntryCount);

	private void Awake() {
		GameObjectPoolController.AddEntry(EntryPoolKey, entryPrefab, maxEntryCount, int.MaxValue);
    }

	public IEnumerator Round() {
		while (bc.units.Count > 0) {				
			Unit unit = GetReadyUnit();

			if (unit == null) {
				IncrementTurnOrder();
				continue;
			}

			bc.turn.Change(unit);

			UpdateTurnOrderUI();

			unit.PostNotification(TurnBeganNotification);

			yield return unit;

			unit.PostNotification(TurnCompletedNotification);
		}
	}

	public void DidActorPerformActionType(ActionType actionType) {
		Stats s = bc.turn.actor.GetComponent<Stats>();
		int cost = TrueActionTypeCost(actionType); 
		s.SetValue(StatTypes.CTR, s[StatTypes.CTR] - cost, false);

		UpdateTurnOrderUI();

		bc.turn.TakeActionType(actionType);
	}

	public void DidActorUndoActionType(ActionType actionType) {
		Stats s = bc.turn.actor.GetComponent<Stats>();
		int cost = TrueActionTypeCost(actionType); 
		s.SetValue(StatTypes.CTR, s[StatTypes.CTR] + cost, false);

		UpdateTurnOrderUI();

		bc.turn.RemoveActionType(actionType);
	}

	public bool CanActorPerformActionType(ActionType actionType) {
		int ctr = GetCounter(bc.turn.actor);
		return ctr >= actionType.Value();
	}

	#endregion

	#region Private

	BattleController bc { get { return GetComponentInParent<BattleController>(); } }

	void IncrementTurnOrder() {
		for (int i = 0; i < bc.units.Count; ++i) {
			Stats s = bc.units[i].GetComponent<Stats>();
			int startingValue = s[StatTypes.CTR];
			if (startingValue < turnActivation)
				s.SetValue(StatTypes.CTR, startingValue + turnStepAmount, true);
		}
	}

	Unit GetReadyUnit() {
		List<Unit> readyUnits = unitsSortedByTurnOrder.Where(
			(u) => CanTakeTurn(u)
		).ToList();

		if (readyUnits.Count == 1)
			return readyUnits.First();

		readyUnits.Sort( (a, b) => GetTurnInitiativeOffset(a).CompareTo(GetTurnInitiativeOffset(b)));

		if (readyUnits.Count == 0)
			return null;
			
		return readyUnits.First();
	}

	List<Unit> unitsSortedByTurnOrder { get {
		var units = new List<Unit>(bc.units).ToList();
		units.Sort( (a,b) => GetTurnInitiativeOffset(a).CompareTo(GetCounter(b)));
		units.Sort( (a,b) => GetCounter(a).CompareTo(GetCounter(b)));
		return units;
	} }

	int TrueActionTypeCost(ActionType actionType) {
		switch(actionType) {
			case ActionType.Wait:
				if (!bc.turn.hasUnitMoved && !bc.turn.hasUnitActed)
					return ActionType.Wait.Value() + turnWaitOnlyPenality;
				goto default;
			default:
 				return actionType.Value();
		}
	}

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
			entry.fontStyle = unit == bc.turn.actor ? FontStyle.Bold : FontStyle.Normal;
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

	int GetTurnInitiativeOffset(Unit target) {
		int tio = target.GetComponent<Stats>().turnInitiativeOffset;
		return tio;
	}

	#endregion
}