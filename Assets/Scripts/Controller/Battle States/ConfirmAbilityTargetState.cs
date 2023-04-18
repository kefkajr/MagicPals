﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ConfirmAbilityTargetState : BattleState
{
	List<Tile> tiles;
	AbilityArea aa;
	int index = 0;

	public override void Enter ()
	{
		base.Enter ();
		aa = turn.ability.GetComponent<AbilityArea>();
		tiles = aa.GetTilesInArea(board, pos);
		board.SelectTiles(tiles);
		FindTargets();
		RefreshPrimaryStatPanel(turn.actor.tile.pos);
		if (turn.targets.Count > 0)
		{
			if (driver.Current == DriverType.Human)
				hitSuccessIndicator.Show();
			SetTarget(0);
		}
		if (driver.Current == DriverType.Computer)
			StartCoroutine(ComputerDisplayAbilitySelection());
	}

	public override void Exit ()
	{
		base.Exit ();
		board.DeSelectTiles(tiles);
		statPanelController.HidePrimary();
		statPanelController.HideSecondary();
		hitSuccessIndicator.Hide();
	}

	protected override void OnMove (object sender, MoveEventData d)
	{
		if (d.point.y > 0 || d.point.x > 0)
			SetTarget(index + 1);
		else
			SetTarget(index - 1);
	}

	protected override void OnSubmit ()
	{
		if (driver.Current == DriverType.Computer) return;
		
		Trap trap = turn.ability.GetComponent<Trap>();
		if (trap != null) {
			owner.ChangeState<TrapSetState>();
		} else if (turn.targets.Count > 0) {
			FindTrueTargets();
			owner.ChangeState<PerformAbilityState>();
		}
	}

	protected override void OnCancel ()
	{
		owner.ChangeState<AbilityTargetState>();
	}

	void FindTargets ()
	{
		turn.targets = new List<Tile>();
		for (int i = 0; i < tiles.Count; ++i)
			if (turn.ability.IsTarget(tiles[i]))
				turn.targets.Add(tiles[i]);
	}

	void FindTrueTargets ()
    {
		// Check to see if, on execution, a different target is hit by the missle.
		ConstantAbilityRange range = turn.ability.GetComponentInChildren<ConstantAbilityRange>();
		if (range != null && range.isMissile)
        {
			Unit targetImpedingMissle = board.UnitImpedingMissile(turn.actor.tile, pos);
			if (targetImpedingMissle != null) {
				board.DeSelectTiles(tiles); // Deselect old tiles
				tiles = aa.GetTilesInArea(board, targetImpedingMissle.tile.pos);
				FindTargets();
			}
		}
		
	}

	void SetTarget (int target)
	{
		index = target;
		if (index < 0)
			index = turn.targets.Count - 1;
		if (index >= turn.targets.Count)
			index = 0;

		if (turn.targets.Count > 0)
		{
			RefreshSecondaryStatPanel(turn.targets[index].pos);
			UpdateHitSuccessIndicator ();
		}
	}

	void UpdateHitSuccessIndicator ()
	{
		int chance = 0;
		int amount = 0;
		Tile target = turn.targets[index];

		Transform obj = turn.ability.transform;
		for (int i = 0; i < obj.childCount; ++i)
		{
			AbilityEffectTarget targeter = obj.GetChild(i).GetComponent<AbilityEffectTarget>();
			if (targeter.IsTarget(target))
			{
				HitRate hitRate = targeter.GetComponent<HitRate>();
				chance = hitRate.Calculate(target);

				BaseAbilityEffect effect = targeter.GetComponent<BaseAbilityEffect>();
				amount = effect.Predict(target);
				break;
			}
		}

		hitSuccessIndicator.SetStats(chance, amount);
	}

	IEnumerator ComputerDisplayAbilitySelection ()
	{
		owner.battleMessageController.Display(turn.ability.name);
		yield return new WaitForSeconds (2f);
		FindTrueTargets();
		owner.ChangeState<PerformAbilityState>();
	}
}