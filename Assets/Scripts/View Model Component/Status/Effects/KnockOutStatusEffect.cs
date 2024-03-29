﻿using UnityEngine;
using System.Collections;

public class KnockOutStatusEffect : StatusEffect {
	Unit owner;
	Stats stats;
	
	void Awake() {
		owner = GetComponentInParent<Unit>();
		stats = owner.GetComponent<Stats>();
	}
	
	void OnEnable() {
		owner.transform.localScale = new Vector3(0.75f, 0.1f, 0.75f);
		this.AddObserver(OnTurnCheck, TurnOrderController.TurnCheckNotification, owner);
		this.AddObserver(OnStatCounterWillChange, Stats.WillChangeNotification(StatTypes.CTR), stats); 
	}
	
	void OnDisable() {
		owner.transform.localScale = Vector3.one;
		this.RemoveObserver(OnTurnCheck, TurnOrderController.TurnCheckNotification, owner);
		this.RemoveObserver(OnStatCounterWillChange, Stats.WillChangeNotification(StatTypes.CTR), stats);
	}
	
	void OnTurnCheck (object sender, object args) {
		// Dont allow a KO'd unit to take turns
		BaseAdjustment adj = args as BaseAdjustment;
		if (adj.defaultToggle == true)
			adj.FlipToggle();
	}
	
	void OnStatCounterWillChange (object sender, object args) {
		// Dont allow a KO'd unit to increment the turn order counter
		ValueChangeAdjustment adj = args as ValueChangeAdjustment;
		if (adj.toValue > adj.fromValue)
			adj.FlipToggle();
	}
}