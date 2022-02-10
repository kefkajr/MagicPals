using UnityEngine;
using System.Collections;

public class StopStatusEffect : StatusEffect 
{
	Stats myStats;

	void OnEnable ()
	{
		myStats = GetComponentInParent<Stats>();
		if (myStats)
			this.AddObserver( OnCounterWillChange, Stats.WillChangeNotification(StatTypes.CTR), myStats );
		this.AddObserver( OnAutomaticHitCheck, HitRate.AutomaticHitCheckNotification );
	}
	
	void OnDisable ()
	{
		this.RemoveObserver( OnCounterWillChange, Stats.WillChangeNotification(StatTypes.CTR), myStats );
		this.RemoveObserver( OnAutomaticHitCheck, HitRate.AutomaticHitCheckNotification );
	}
	
	void OnCounterWillChange (object sender, object args)
	{
		ValueChangeAdjustment adj = args as ValueChangeAdjustment;
		adj.FlipToggle();
	}

	void OnAutomaticHitCheck (object sender, object args)
	{
		Unit owner = GetComponentInParent<Unit>();
		MatchAdjustment adj = args as MatchAdjustment;
		if (owner == adj.target)
			adj.FlipToggle();
	}
}
