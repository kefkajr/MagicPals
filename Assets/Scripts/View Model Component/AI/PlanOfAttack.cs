using UnityEngine;
using System.Collections;

/* As soon as it is determined that it is the computer’s turn to make a move,
 * we will need to formulate a plan of attack. This means I decide what ability to use,
 * who or what to use the ability on, where I move to on the board,
 * and where I cast the ability or which direction I face while casting the ability.
 * All of this data is stored in a simple object which will be populated by various steps of the AI process. */

public class PlanOfAttack 
{
	public Ability ability;
	public Targets target;
	public Tile moveLocation;
	public Tile fireLocation;
	public Directions attackDirection;
}
