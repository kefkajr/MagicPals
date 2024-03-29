﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CommandSelectionState : BaseAbilityMenuState {
	class Option {
		public static string Move = "Move";
		public static string Action = "Action";
		public static string Item = "Item";
		public static string PickUp = "Pick up";
		public static string Escape = "Escape";
		public static string Wait = "Wait";
	}

	TurnOrderController toc { get { return owner.turnOrderController; } }

	public override void Enter() {
		base.Enter();
		statPanelController.ShowPrimary(turn.actor.gameObject);
		if (driver.Current == DriverType.Computer)
			StartCoroutine( ComputerTurn() );

		// Allow the unit to perceive in whatever direction they start facing
		owner.awarenessController.Look(turn.actor);
		owner.awarenessController.ClearAwarenessLines();
		board.DeHighlightAllTiles();
	}

	public override void Exit() {
		base.Exit();
		statPanelController.HidePrimary();
	}

	protected override void LoadMenu () {
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
		if (owner.levelData.exits.Contains(owner.turn.actor.tile.pos))
			menuOptions.Add(Option.Escape);
		menuOptions.Add(Option.Wait);

		abilityMenuPanelController.Show(menuTitle, menuOptions);
		abilityMenuPanelController.SetLocked(menuOptions.IndexOf(Option.Move), !toc.CanActorPerformActionType(ActionType.Move));
		abilityMenuPanelController.SetLocked(menuOptions.IndexOf(Option.Action), !toc.CanActorPerformActionType(ActionType.Major));

		Inventory inventory = turn.actor.GetComponentInChildren<Inventory>();
		abilityMenuPanelController.SetLocked(menuOptions.IndexOf(Option.Item), inventory.items.Count < 1);
	}

	protected override void OnSubmit() {
		if (driver.Current == DriverType.Computer) return;
		
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
		} else if (selectedOption == Option.Escape) {
			owner.GetState<FlexibleOptionState>().SetTitle("Escape");
			List<FlexibleOption> options = new List<FlexibleOption> {
				new FlexibleOption("Escape", "Leave the area and do not return", delegate() {
					owner.ChangeState<EscapeState>();
				}),
				new FlexibleOption("Do not Escape", "Stay in the area", delegate() {
					owner.ChangeState<CommandSelectionState>();
				})
			 };
			FlexibleOptionState.flexibleOptions = options;
			owner.ChangeState<FlexibleOptionState>();
		} else if (selectedOption == Option.Wait) {
			owner.ChangeState<EndFacingState>();
		}
	}

	protected override void OnCancel() {
		if (turn.hasUnitMoved && !turn.lockMove) {
			owner.turnOrderController.DidActorUndoActionType(ActionType.Move);
			SelectTile(turn.actor.tile.pos);
			LoadMenu();
		} else {
			owner.ChangeState<ExploreState>();
		}
	}

	IEnumerator ComputerTurn () {
		if (turn.plan == null) {
			yield return owner.cpu.Evaluate(turn);
			turn.ability = turn.plan.ability;
		}

		yield return new WaitForSeconds (1f);

		bool unitShouldMove = turn.plan.moveLocation != turn.actor.tile && turn.plan.moveLocation != null;

		if (turn.hasUnitMoved == false && unitShouldMove)
			owner.ChangeState<MoveTargetState>();
		else if (turn.hasUnitActed == false && turn.plan.ability != null)
			owner.ChangeState<AbilityTargetState>();
		else
			owner.ChangeState<EndFacingState>();
	}
}