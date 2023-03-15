using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class EscapeState : BattleState
{
	public override void Enter()
	{
		base.Enter();
        StartCoroutine(AnimateEscape());
	}

	public override void Exit()
	{
		base.Exit();
	}

    IEnumerator AnimateEscape() {
        Console.Main.Log(string.Format("{0} has escaped.", turn.actor.name));
        yield return new WaitForSeconds(0.5f);
        
        Unit actor = turn.actor;
        owner.awarenessController.HandleUnitEscape(actor);

        if (IsBattleOver())
			owner.ChangeState<CutSceneState>();
		else
			owner.ChangeState<SelectUnitState>();
    }
}
