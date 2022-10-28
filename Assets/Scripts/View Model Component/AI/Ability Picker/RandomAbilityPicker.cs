using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Because I had mentioned that it would be nice to add a little variety to the attack sequence,
 * we can help break up the pattern a little by adding some randomly chosen abilities.
 * Note that instead of maintaining pairs of ability names and targets,
 * I simply refer to a list of other ability pickers.
 * In most cases those will be FixedAbilityPickers,
 * but we could also nest other complex types of pickers if we wanted to.
 * Then we randomly grab one of the pickers and return the value that the selected picker holds. */
public class RandomAbilityPicker : BaseAbilityPicker
{
	public List<BaseAbilityPicker> pickers;

	public override void Pick (PlanOfAttack plan)
	{
		int index = Random.Range(0, pickers.Count);
		BaseAbilityPicker p = pickers[index];
		p.Pick(plan);
	}
}