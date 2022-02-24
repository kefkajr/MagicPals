using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemOptionState : BaseAbilityMenuState
{
	public static Merchandise item;
	Inventory inventory;

	class Option
    {
		public static string Equip = "Equip";
		public static string Unequip = "Unequip";
		public static string Discard = "Discard";
	}

	public override void Enter()
	{
		base.Enter();
		statPanelController.ShowPrimary(turn.actor.gameObject);
	}

	public override void Exit()
	{
		base.Exit();
		statPanelController.HidePrimary();
	}

	protected override void LoadMenu()
	{
		inventory = turn.actor.GetComponentInChildren<Inventory>();

		if (menuOptions == null)
			menuOptions = new List<string>();
		else
			menuOptions.Clear();

		menuTitle = item.name;

		Equippable equippable = item as Equippable;
		if (equippable != null)
        {
			if (equippable.isEquipped)
				menuOptions.Add(Option.Unequip);
			else
				menuOptions.Add(Option.Equip);
		}

		menuOptions.Add(Option.Discard);

		abilityMenuPanelController.Show(menuTitle, menuOptions);
	}

	protected override void Confirm()
	{
		int currentSelection = abilityMenuPanelController.selection;
		string selectedOption = menuOptions[currentSelection];

		Equippable equippable = item as Equippable;
		if (selectedOption == Option.Equip)
			inventory.Equip(equippable, equippable.defaultSlots);
		else if (selectedOption == Option.Unequip)
			inventory.UnEquip(equippable);
		else if (selectedOption == Option.Discard)
        {
			if (equippable != null)
				inventory.UnEquip(equippable);
			inventory.Discard(item);
        }

		if (inventory.items.Count > 0)
			owner.ChangeState<ItemSelectionState>();
		else
			owner.ChangeState<CommandSelectionState>();
	}

	protected override void Cancel()
	{
		owner.ChangeState<ItemSelectionState>();
	}
}
