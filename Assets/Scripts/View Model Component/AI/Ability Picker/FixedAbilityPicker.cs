using UnityEngine;
using System.Collections;

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
	public Targets target;
	public string ability;

	public override void Pick (PlanOfAttack plan)
	{
		plan.target = target;
		plan.ability = Find(ability);

		if (plan.ability == null)
		{
			plan.ability = Default();
			plan.target = Targets.Foe;
		}
	}
}