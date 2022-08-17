using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CommandSelectionState : BaseAbilityMenuState
{
	class Option
	{
		public static string Move = "Move";
		public static string Action = "Action";
		public static string Item = "Item";
		public static string PickUp = "Pick up";
		public static string Wait = "Wait";
	}

	public override void Enter ()
	{
		base.Enter ();
		statPanelController.ShowPrimary(turn.actor.gameObject);
		if (driver.Current == Drivers.Computer)
			StartCoroutine( ComputerTurn() );

		// Allow the unit to perceive in whatever direction they start facing
		Perception perception = turn.actor.GetComponent<Perception>();
		perception.Perceive(board: board);
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

		menuOptions.Add(Option.Move);
		menuOptions.Add(Option.Action);
		menuOptions.Add(Option.Item);
		if (owner.boardInventory.GetItemsByPoint(turn.actor.tile.pos).Count > 0)
			menuOptions.Add(Option.PickUp);
		menuOptions.Add(Option.Wait);

		abilityMenuPanelController.Show(menuTitle, menuOptions);
		abilityMenuPanelController.SetLocked(menuOptions.IndexOf(Option.Move), turn.hasUnitMoved);
		abilityMenuPanelController.SetLocked(menuOptions.IndexOf(Option.Action), turn.hasUnitActed);

		Inventory inventory = turn.actor.GetComponentInChildren<Inventory>();
		abilityMenuPanelController.SetLocked(menuOptions.IndexOf(Option.Item), inventory.items.Count < 1);
	}

	protected override void Confirm ()
	{
		int currentSelection = abilityMenuPanelController.selection;
		string selectedOption = menuOptions[currentSelection];
		if (selectedOption == Option.Move) {
			owner.ChangeState<MoveTargetState>();
		} else if (selectedOption == Option.Action) {
			owner.ChangeState<CategorySelectionState>();
		} else if (selectedOption == Option.Item) {
			owner.ChangeState<ItemSelectionState>();
		} else if (selectedOption == Option.PickUp) {
			ItemPickUpState.boardInventory = owner.boardInventory;
			ItemPickUpState.point = turn.actor.tile.pos;
			owner.ChangeState<ItemPickUpState>();
		} else if (selectedOption == Option.Wait) {
			owner.ChangeState<EndFacingState>();
		}
	}

	protected override void Cancel ()
	{
		if (turn.hasUnitMoved && !turn.lockMove)
		{
			turn.UndoMove();
			SelectTile(turn.actor.tile.pos);
			LoadMenu();
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