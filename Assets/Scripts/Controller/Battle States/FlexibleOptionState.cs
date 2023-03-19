using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class FlexibleOptionState : BaseAbilityMenuState
{
	// Set menu title before entering this state

	public static List<FlexibleOption> flexibleOptions;

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

	public void SetTitle(string title) {
		menuTitle = title;
	}

	protected override void LoadMenu()
	{
		if (menuOptions == null)
			menuOptions = new List<string>();
		else
			menuOptions.Clear();

		for (int i = 0; i < flexibleOptions.Count; ++i)
			menuOptions.Add(flexibleOptions[i].title);

		abilityMenuPanelController.Show(menuTitle, menuOptions);

		DisplayFlexibleOptionInfo();
	}

	protected override void OnSubmit()
	{
		Action Action = flexibleOptions[abilityMenuPanelController.selection].action;
		Action();
	}

	protected override void OnCancel()
	{
		owner.ChangeState<CommandSelectionState>();
	}

	protected override void OnMove(object sender, MoveEventData d)
	{
		base.OnMove(sender, d);
		DisplayFlexibleOptionInfo();
	}

	private void DisplayFlexibleOptionInfo() {
		FlexibleOption option = flexibleOptions[abilityMenuPanelController.selection];
		descriptionPanelController.Show(option.title, option.description);
	}
}


public struct FlexibleOption {
	public string title;
	public string description;
	public Action action;

	public FlexibleOption(string title, string description, Action action) {
		this.title = title;
		this.description = description;
		this.action = action;
	}
}
