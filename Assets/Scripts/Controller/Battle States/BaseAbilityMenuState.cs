using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class BaseAbilityMenuState : BattleState
{
	protected string menuTitle;
	protected List<string> menuOptions;

	public override void Enter ()
	{
		base.Enter ();
		SelectTile(turn.actor.tile.pos);
		if (driver.Current == Drivers.Human)
			LoadMenu();
	}

	public override void Exit ()
	{
		base.Exit ();
		abilityMenuPanelController.Hide();
		descriptionPanelController.Hide();
	}

	protected override void OnFire (object sender, InfoEventArgs<int> e)
	{
		if (e.info == 0)
			Confirm();
		else
			Cancel();
	}

	protected override void OnMove(object sender, MoveEventData moveEventData)
	{
		if (moveEventData.point.x > 0 || moveEventData.point.y < 0)
			abilityMenuPanelController.Next();
		else
			abilityMenuPanelController.Previous();
	}

	protected abstract void LoadMenu ();
	protected abstract void Confirm ();
	protected abstract void Cancel ();
}