using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/* Next we need a component which can organize all of our gambits into the actual sequential list
 * and keep track of its own position within that list.
 
 At this point you can begin creating resources of “Attack Pattern” prefabs.
The basic idea is that you create an empty game object with the attack gambitSet script attached.
Then add children gameobjects, one for each ability gambit.

Whenever I create one of the Random Gambit types,
I first would create all of the Fixed types which it would refer to.
By keeping each gambit on its own gameobject
it is very easy to drag and drop them in the inspector to the compound gambit types
and/or to the attack gambitSet list in the root.

This is still prototype level work,
so I think it’s good to just manually create what we want to work with.
When I have verified that the system works well and is sufficiently flexible,
it would be nice to come up with a way to create them by simpler “recipes”
and allow a factory to automatically generate them like we do with ability catalogs.*/

public class GambitSet : MonoBehaviour 
{
	public List<Gambit> gambits;
	
	public Gambit PickGambit (BattleController bc, Func<Gambit, bool> CanGambitAbilityBeUsed)
	{
		foreach (Gambit gambit in gambits) {
			if (gambit.IsViable(bc) && CanGambitAbilityBeUsed(gambit)) {
				return gambit;
			}
		}
		return null;
	}
}
