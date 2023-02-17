using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* The primary “picker” used by this first implementation will be
 * one which says exactly what we want to use.
 * This goes back to the sequential list idea where I specify something
 * like “Attack, Fire, Heal” and it will use those specific abilities. */

/* We accomplish the job by exposing two fields,
 * the name of an ability and who or what we want to target with that ability.
 * Then in the Pick method, we use the base class’s Find method to get
 * the actual ability of the same name and then update the plan of attack with our decisions.
 * If the named ability can’t be found, we grab the first ability it can find instead
 * (which would be Attack). */
public class FixedAbilityPicker : BaseAbilityPicker
{
	public TargetType targetType;
	public AbilityPickerCriteria abilityPickerCriteria;
	public string ability;

    public override bool IsViable(BattleController bc)
    {
        return abilityPickerCriteria.IsFulfilled(bc, owner, targetType);
    }

    public override void Pick (PlanOfAttack plan)
	{
		plan.targetType = targetType;
		plan.ability = Find(ability);

		if (plan.ability == null)
		{
			plan.ability = Default();
			plan.targetType = TargetType.Foe;
		}
	}
}


public enum AbilityPickerCriteria {
	IsSeen,
    IsNotAwareOfTheSameFoes
}

public static class AbilityPickerCriteriaExtensions
{
    public static bool IsFulfilled(this AbilityPickerCriteria apc, BattleController bc, Unit actor, TargetType targetType)
	{
		switch (apc)
		{
			case AbilityPickerCriteria.IsSeen:
				List<Awareness> topAwarenesses = bc.awarenessController.TopAwarenesses(actor);
				foreach (Awareness awareness in topAwarenesses) {
					if (awareness.type == AwarenessType.Seen)
						return true;
				}
				return false;
			case AbilityPickerCriteria.IsNotAwareOfTheSameFoes:
                topAwarenesses = bc.awarenessController.TopAwarenesses(actor);
                foreach (Unit perceiver in bc.awarenessController.awarenessMap.Keys) {
                    Alliance actorAliance = actor.GetComponent<Alliance>();
                    Alliance perceiverAlliance = perceiver.GetComponent<Alliance>();

                    if (!actorAliance.IsMatch(perceiverAlliance, targetType))
                        continue; // Go to next unit in awareness map
                    
                    // If the actor has seen any foes that an ally has not seen, the criteria is fufilled
                    foreach (Awareness awareness in topAwarenesses) {
                        if (actorAliance.IsMatch(awareness.stealth.GetComponent<Alliance>(), targetType))
                            continue;

                        if (awareness.type == AwarenessType.Seen &&
                            !bc.awarenessController.IsAwareOfUnit(perceiver, awareness.stealth.unit, new AwarenessType[] {AwarenessType.MayHaveHeard, AwarenessType.MayHaveSeen, AwarenessType.Seen})) {
                            return true;
                        }
                    }
                }
                return false;
			default:
				return false;
		};
	}
}
