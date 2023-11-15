using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* As soon as it is determined that it is the computer’s turn to make a move,
 * we will need to formulate a turn plan. This means I decide what ability to use,
 * who or what to use the ability on, where I move to on the board,
 * and where I cast the ability or which direction I face while casting the ability.
 * All of this data is stored in a simple object which will be populated by various steps of the AI process. */

public class TurnPlan {
	public Ability ability;
	public TargetType targetType;
	public Tile moveLocation;
	public Tile fireLocation;
	public Direction attackDirection;

	public TurnPlan() {}
	public TurnPlan(Gambit gambit) {
		targetType = gambit.targetType;
		ability = gambit.ability;
	}
}
