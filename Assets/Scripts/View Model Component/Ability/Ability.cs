using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Ability : MonoBehaviour 
{
	public const string CanPerformCheck = "Ability.CanPerformCheck";
	public const string FailedNotification = "Ability.FailedNotification";
	public const string DidPerformNotification = "Ability.DidPerformNotification";

	public Describable describable { get { return GetComponent<Describable>(); } }

	public bool CanPerform ()
	{
		BaseAdjustment adj = new BaseAdjustment(true);
		this.PostNotification(CanPerformCheck, adj);
		return adj.toggle;
	}

	public void Perform (List<Tile> targets)
	{
		if (!CanPerform())
		{
			this.PostNotification(FailedNotification);
			return;
		}

		for (int i = 0; i < targets.Count; ++i)
			Perform(targets[i]);

		// If the ability is attached to a consumable item, consume the item
		Merchandise merchandise = GetComponentInParent<Merchandise>();
		Consumable consumable = merchandise != null ? merchandise.GetComponentInChildren<Consumable>() : null;
		if (consumable != null)
			consumable.Consume();

		this.PostNotification(DidPerformNotification);
	}

	public bool IsTarget (Tile tile)
	{
		Transform obj = transform;
		for (int i = 0; i < obj.childCount; ++i)
		{
			AbilityEffectTarget targeter = obj.GetChild(i).GetComponent<AbilityEffectTarget>();
			if (targeter != null && targeter.IsTarget(tile))
				return true;
		}
		return false;
	}

	void Perform (Tile target)
	{
		for (int i = 0; i < transform.childCount; ++i)
		{
			Transform child = transform.GetChild(i);
			BaseAbilityEffect effect = child.GetComponent<BaseAbilityEffect>();
			effect.Apply(target);
		}
	}
}