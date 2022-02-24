using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemOptionState : BaseAbilityMenuState
{
	public static Merchandise item;
	Inventory inventory;

	class Option
    {
		public static string Use = "Use";
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

		Ability ability = item.GetComponentInChildren<Ability>();
		if (ability != null)
			menuOptions.Add(Option.Use);

		Equippable equippable = item.GetComponentInChildren<Equippable>();
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

		Ability ability = item.GetComponentInChildren<Ability>();
		Equippable equippable = item.GetComponentInChildren<Equippable>();
		if (selectedOption == Option.Use)
		{
			turn.ability = ability;
			owner.ChangeState<AbilityTargetState>();
		}
		else if (selectedOption == Option.Equip)
		{
			inventory.Equip(equippable, equippable.defaultSlots);
			owner.ChangeState<ItemSelectionState>();
		}
		else if (selectedOption == Option.Unequip)
		{
			inventory.UnEquip(equippable);
			owner.ChangeState<ItemSelectionState>();
		}
		else if (selectedOption == Option.Discard)
		{
			if (equippable != null)
				inventory.UnEquip(equippable);
			inventory.Discard(item);

			// Either reweind to 
			if (inventory.items.Count > 0)
				owner.ChangeState<ItemSelectionState>();
			else
				owner.ChangeState<CommandSelectionState>();
		}
	}

	protected override void Cancel()
	{
		owner.ChangeState<ItemSelectionState>();
	}
}
