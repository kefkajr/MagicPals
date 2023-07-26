using UnityEngine;
using System.Collections;

public class DurationStatusCondition : StatusCondition {
	public int duration = 10;

	void OnEnable() {
		this.AddObserver(OnNewTurn, TurnOrderController.TurnBeganNotification);
	}

	void OnDisable() {
		this.RemoveObserver(OnNewTurn, TurnOrderController.TurnCompletedNotification);
	}

	void OnNewTurn (object sender, object args) {
		duration--;
		if (duration <= 0)
			Remove();
	}
}
