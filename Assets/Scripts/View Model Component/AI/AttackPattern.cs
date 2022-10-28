using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Next we need a component which can organize all of our pickers into the actual sequential list
 * and keep track of its own position within that list.
 
 At this point you can begin creating resources of “Attack Pattern” prefabs.
The basic idea is that you create an empty game object with the attack pattern script attached.
Then add children gameobjects, one for each ability picker.

Whenever I create one of the Random Ability Picker types,
I first would create all of the Fixed types which it would refer to.
By keeping each picker on its own gameobject
it is very easy to drag and drop them in the inspector to the compound picker types
and/or to the attack pattern list in the root.

This is still prototype level work,
so I think it’s good to just manually create what we want to work with.
When I have verified that the system works well and is sufficiently flexible,
it would be nice to come up with a way to create them by simpler “recipes”
and allow a factory to automatically generate them like we do with ability catalogs.*/

public class AttackPattern : MonoBehaviour 
{
	public List<BaseAbilityPicker> pickers;
	int index;
	
	public void Pick (PlanOfAttack plan)
	{
		pickers[index].Pick(plan);
		index++;
		if (index >= pickers.Count)
			index = 0;
	}
}
