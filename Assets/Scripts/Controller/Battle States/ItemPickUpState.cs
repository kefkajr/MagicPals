using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemPickUpState : BaseAbilityMenuState
{
	public static BoardInventory boardInventory;
	public static Point point;

	public override void Enter()
	{
		base.Enter();
		statPanelController.ShowPrimary(turn.actor.gameObject);
	}

	public override void Exit()
	{
		base.Exit();
		statPanelController.HidePrimary();
		itemDescriptionPanelController.Hide();
	}

	protected override void LoadMenu()
	{
		if (menuOptions == null)
			menuOptions = new List<string>();
		else
			menuOptions.Clear();

		menuTitle = "Pick up";

		List<Merchandise> items = boardInventory.GetItemsByPoint(point);
		for (int i = 0; i < items.Count; ++i)
			menuOptions.Add(items[i].name);

		abilityMenuPanelController.Show(menuTitle, menuOptions);
		itemDescriptionPanelController.Show(items[0]);
	}

	protected override void Confirm()
	{
		Merchandise item = GetCurrentlySelectedItem();
		boardInventory.RemoveByPoint(item, point);

		Inventory actorInventory = turn.actor.GetComponentInChildren<Inventory>();
		actorInventory.Add(item);

		owner.ChangeState<CommandSelectionState>();
	}

	protected override void Cancel()
	{
		owner.ChangeState<CommandSelectionState>();
	}

	protected override void OnMove(object sender, InfoEventArgs<Point> e)
	{
		base.OnMove(sender, e);
		Merchandise item = GetCurrentlySelectedItem();
		itemDescriptionPanelController.Show(item);
	}

	private Merchandise GetCurrentlySelectedItem()
    {
		List<Merchandise> items = boardInventory.GetItemsByPoint(point);
		Merchandise item = items[abilityMenuPanelController.selection];
		return item;
	}
}
