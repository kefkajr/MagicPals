using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* The primary gambit used by this first implementation will be
 * one which says exactly what we want to use.
 * This goes back to the sequential list idea where I specify something
 * like “Attack, Fire, Heal” and it will use those specific abilities. */

/* We accomplish the job by exposing two fields,
 * the name of an ability and who or what we want to target with that ability.
 * Then in the Pick method, we use the base class’s Find method to get
 * the actual ability of the same name and then update the turn plan with our decisions.
 * If the named ability can’t be found, we grab the first ability it can find instead
 * (which would be Attack). */
public class Gambit : MonoBehaviour {
	public TargetType targetType;
	public GambitCriteria gambitCriteria;
	public string abilityName;
	public  Unit owner;
	public AbilityCatalog ac;
	public Ability ability { get { return FindAbility(); } }

	void Start() {
		owner = GetComponentInParent<Unit>();
		ac = owner.GetComponentInChildren<AbilityCatalog>();
	}

    public bool IsViable(BattleController bc) {
        return gambitCriteria.IsFulfilled(bc, owner, targetType);
    }


	protected Ability FindAbility() {
		for (int i = 0; i < ac.transform.childCount; ++i)
		{
			Transform category = ac.transform.GetChild(i);
			Transform child = category.Find(abilityName);
			if (child != null)
				return child.GetComponent<Ability>();
		}
		return null;
	}
}


public enum GambitCriteria {
	IsSeen,
    IsNotAwareOfTheSameFoes
}

public static class GambitCriteriaExtensions {
    public static bool IsFulfilled(this GambitCriteria apc, BattleController bc, Unit actor, TargetType targetType) {
		switch (apc) {
			case GambitCriteria.IsSeen:
				List<Awareness> topAwarenesses = bc.awarenessController.TopAwarenesses(actor);
				foreach (Awareness awareness in topAwarenesses) {
					if (awareness.type == AwarenessType.Seen)
						return true;
				}
				return false;
			case GambitCriteria.IsNotAwareOfTheSameFoes:
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
