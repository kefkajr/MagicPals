using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndFacingState : BattleState 
{
	Directions startDir;

	public override void Enter ()
	{
		base.Enter ();
		startDir = turn.actor.dir;
		SelectTile(turn.actor.tile.pos);
		owner.facingIndicator.gameObject.SetActive(true);
		owner.facingIndicator.SetDirection(turn.actor.dir);
		if (driver.Current == DriverType.Computer)
			StartCoroutine(ComputerControl());
	}

	public override void Exit ()
	{
		owner.facingIndicator.gameObject.SetActive(false);
		base.Exit ();
	}
	
	protected override void OnMove (object sender, MoveEventData d)
	{
		turn.actor.dir = d.pointTranslatedByCameraDirection.GetDirection();
		turn.actor.Match();
		owner.facingIndicator.SetDirection(turn.actor.dir);

		LetActorLookInCurrentDirection();
	}
	
	protected override void OnFire (object sender, InfoEventArgs<int> e)
	{
		switch (e.info)
		{
		case 0:
			owner.ChangeState<SelectUnitState>();
			break;
		case 1:
			turn.actor.dir = startDir;
			turn.actor.Match();
			owner.ChangeState<CommandSelectionState>();
			break;
		}
	}

	IEnumerator ComputerControl ()
	{
		yield return new WaitForSeconds(0.5f);
		turn.actor.dir = owner.cpu.DetermineEndFacingDirection();
		turn.actor.Match();
		owner.facingIndicator.SetDirection(turn.actor.dir);

		LetActorLookInCurrentDirection();

		yield return new WaitForSeconds(0.5f);
		owner.ChangeState<SelectUnitState>();
	}

	void LetActorLookInCurrentDirection()
    {
		// Allow the unit to perceive in whatever direction they turn
		owner.awarenessController.Look(turn.actor);
	}
}