using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class BaseAbilityMenuState : BattleState {
	protected string menuTitle;
	protected List<string> menuOptions;

	public override void Enter() {
		base.Enter();
		SelectTile(turn.actor.tile.pos);
		if (driver.Current == DriverType.Human)
			LoadMenu();
	}

	public override void Exit() {
		base.Exit();
		abilityMenuPanelController.Hide();
		descriptionPanelController.Hide();
	}

	protected override void OnMove(object sender, MoveEventData moveEventData) {
		if (moveEventData.point.x > 0 || moveEventData.point.y < 0)
			abilityMenuPanelController.Next();
		else
			abilityMenuPanelController.Previous();
	}

	protected override void OnPoint (object sender, Vector2 v) {
		// Found out which entry has the pointer of it and highlight it.
		for(int i = 0; i < abilityMenuPanelController.menuEntries.Count; i ++) {
			var entry = abilityMenuPanelController.menuEntries[i];
			if (RaycastUtilities.IsPointerOverUIObject(v, entry.gameObject)) {
				abilityMenuPanelController.SetSelection(i);
				return;
			}
		}

		// If pointer wanders off of menu panel, deselect all entries;
		if (RaycastUtilities.IsPointerOverUIObject(v, abilityMenuPanelController.panel.gameObject)) {
			abilityMenuPanelController.Deselect();
		}
	}

	protected override void OnClick (object sender, Vector2 v) {
		if(RaycastUtilities.IsPointerOverUIObject(v, abilityMenuPanelController.panel.gameObject)) {
			OnSubmit();
		} else {
			OnCancel();
		}
	}

	protected abstract void LoadMenu ();
}