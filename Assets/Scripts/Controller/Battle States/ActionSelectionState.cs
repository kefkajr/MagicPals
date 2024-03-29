﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ActionSelectionState : BaseAbilityMenuState {
	public static int category;
	AbilityCatalog catalog;

	public override void Enter() {
		base.Enter();
		statPanelController.ShowPrimary(turn.actor.gameObject);
	}
	
	public override void Exit() {
		base.Exit();
		statPanelController.HidePrimary();
	}

	protected override void LoadMenu () {
		catalog = turn.actor.GetComponentInChildren<AbilityCatalog>();
		GameObject container = catalog.GetCategory(category);
		menuTitle = container.name;

		int count = catalog.AbilityCount(container);
		if (menuOptions == null)
			menuOptions = new List<string>(count);
		else
			menuOptions.Clear();

		bool[] locks = new bool[count];
		for (int i = 0; i < count; ++i) {
			Ability ability = catalog.GetAbility(category, i);
			AbilityStatCost cost = ability.GetComponent<AbilityStatCost>();
			if (cost)
				menuOptions.Add(string.Format("{0}: {1}", ability.name, cost.amount));
			else
				menuOptions.Add(ability.name);
			locks[i] = !ability.CanPerform();
		}

		abilityMenuPanelController.Show(menuTitle, menuOptions);
		for (int i = 0; i < count; ++i)
			abilityMenuPanelController.SetLocked(i, locks[i]);

		descriptionPanelController.Show(catalog.GetAbility(category, 0).describable);
	}

	protected override void OnSubmit() {
		int currentSelection = abilityMenuPanelController.selection;
		if (!abilityMenuPanelController.GetLocked(currentSelection)) {
			turn.ability = catalog.GetAbility(category, currentSelection);
			owner.ChangeState<AbilityTargetState>();
		}
	}

	protected override void OnCancel() {
		owner.ChangeState<CategorySelectionState>();
	}

	protected override void OnMove(object sender, MoveEventData moveEventData){
		base.OnMove(sender, moveEventData);
		ShowAbilityDescription();
	}

	protected override void OnPoint(object sender, Vector2 v) {
		base.OnPoint(sender, v);
		ShowAbilityDescription();
	}

	void ShowAbilityDescription() {
		int currentSelection = abilityMenuPanelController.selection;
		Ability ability = catalog.GetAbility(category, currentSelection);
		descriptionPanelController.Show(ability.describable);
	}
}