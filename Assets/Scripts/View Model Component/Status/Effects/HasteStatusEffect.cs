using UnityEngine;
using System.Collections;

public class HasteStatusEffect : StatusEffect {
	Stats myStats;

	void OnEnable() {
		myStats = GetComponentInParent<Stats>();
		if (myStats)
			this.AddObserver( OnCounterWillChange, Stats.WillChangeNotification(StatTypes.CTR), myStats );
	}

	void OnDisable() {
		this.RemoveObserver( OnCounterWillChange, Stats.WillChangeNotification(StatTypes.CTR), myStats );
	}

	void OnCounterWillChange (object sender, object args) {
		ValueChangeAdjustment adj = args as ValueChangeAdjustment;
		MultDeltaModifier m = new MultDeltaModifier(0, 2);
		adj.AddModifier(m);
	}
}