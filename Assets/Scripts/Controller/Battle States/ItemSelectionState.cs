using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemSelectionState : BaseAbilityMenuState {
	Inventory inventory;

	public override void Enter() {
		base.Enter();
		statPanelController.ShowPrimary(turn.actor.gameObject);
	}

	public override void Exit() {
		base.Exit();
		statPanelController.HidePrimary();
	}

	protected override void LoadMenu() {
		inventory = turn.actor.GetComponentInChildren<Inventory>();

		if (menuOptions == null)
			menuOptions = new List<string>();
		else
			menuOptions.Clear();

		menuTitle = "Item";

		for (int i = 0; i < inventory.items.Count; ++i)
			menuOptions.Add(inventory.items[i].name);

		abilityMenuPanelController.Show(menuTitle, menuOptions);
		Describable describable = inventory.items[0].describable;
		descriptionPanelController.Show(describable);
	}

	protected override void OnSubmit() {
		Merchandise item = inventory.items[abilityMenuPanelController.selection];
		ItemOptionState.item = item;

		owner.ChangeState<ItemOptionState>();
	}

	protected override void OnCancel() {
		owner.ChangeState<CommandSelectionState>();
	}

	protected override void OnMove(object sender, MoveEventData d) {
		base.OnMove(sender, d);
		ShowItemDescription();
	}

	protected override void OnPoint(object sender, Vector2 v) {
		base.OnPoint(sender, v);
		ShowItemDescription();
	}

	void ShowItemDescription() {
		Merchandise item = inventory.items[abilityMenuPanelController.selection]; ;
		descriptionPanelController.Show(item.describable);
	}

}
