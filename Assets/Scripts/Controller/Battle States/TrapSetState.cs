using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapSetState : BattleState {
	public override void Enter() {
		base.Enter();
		owner.turnOrderController.DidActorPerformActionType(ActionType.Major);
		
		if (turn.hasUnitMoved)
			turn.lockMove = true;
		StartCoroutine(Animate());
	}

	IEnumerator Animate() {
		// TODO play animations, etc
		yield return null;
		SetTrap();

		if (IsBattleOver())
			owner.ChangeState<CutSceneState>();
		else if (!UnitCanReceiveCommands())
			owner.ChangeState<SelectUnitState>();
		else if (turn.hasUnitMoved)
			owner.ChangeState<EndFacingState>();
		else
			owner.ChangeState<CommandSelectionState>();
	}

	void SetTrap() {
		Tile tile = currentTile;
		tile.trap = turn.ability.GetComponent<Trap>();
    }
}

