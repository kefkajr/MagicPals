using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CommandSelectionState : BaseAbilityMenuState 
{
	public override void Enter ()
	{
		base.Enter ();
		statPanelController.ShowPrimary(turn.actor.gameObject);
		if (driver.Current == Drivers.Computer)
			StartCoroutine( ComputerTurn() );
	}

	public override void Exit ()
	{
		base.Exit ();
		statPanelController.HidePrimary();
	}

	protected override void LoadMenu ()
	{
		if (menuOptions == null)
		{
			menuTitle = "Commands";
			menuOptions = new List<string>();
		}	
		else
			menuOptions.Clear();

		menuOptions.Add("Move");
		menuOptions.Add("Action");
		menuOptions.Add("Item");
		menuOptions.Add("Wait");

		abilityMenuPanelController.Show(menuTitle, menuOptions);
		abilityMenuPanelController.SetLocked(0, turn.hasUnitMoved);
		abilityMenuPanelController.SetLocked(1, turn.hasUnitActed);

		Inventory inventory = turn.actor.GetComponentInChildren<Inventory>();
		abilityMenuPanelController.SetLocked(2, inventory.items.Count < 1);
	}

	protected override void Confirm ()
	{
		switch (abilityMenuPanelController.selection)
		{
		case 0: // Move
			owner.ChangeState<MoveTargetState>();
			break;
		case 1: // Action
			owner.ChangeState<CategorySelectionState>();
			break;
		case 2: // Item
			owner.ChangeState<ItemSelectionState>();
			break;
		case 3: // Wait
			owner.ChangeState<EndFacingState>();
			break;
		}
	}

	protected override void Cancel ()
	{
		if (turn.hasUnitMoved && !turn.lockMove)
		{
			turn.UndoMove();
			abilityMenuPanelController.SetLocked(0, false);
			SelectTile(turn.actor.tile.pos);
		}
		else
		{
			owner.ChangeState<ExploreState>();
		}
	}

	IEnumerator ComputerTurn ()
	{
		if (turn.plan == null)
		{
			turn.plan = owner.cpu.Evaluate();
			turn.ability = turn.plan.ability;
		}

		yield return new WaitForSeconds (1f);

		if (turn.hasUnitMoved == false && turn.plan.moveLocation != turn.actor.tile.pos)
			owner.ChangeState<MoveTargetState>();
		else if (turn.hasUnitActed == false && turn.plan.ability != null)
			owner.ChangeState<AbilityTargetState>();
		else
			owner.ChangeState<EndFacingState>();
	}
}