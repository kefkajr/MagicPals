using UnityEngine;
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

	protected override void OnFire (object sender, InfoEventArgs<int> e)
	{
		if (e.info == 0)
		{
			Trap trap = turn.ability.GetComponent<Trap>();
			if (trap != null) {
				owner.ChangeState<TrapSetState>();
            } else if (turn.targets.Count > 0) {
				FindTrueTargets();
				owner.ChangeState<PerformAbilityState>();
			}
		}
		else
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
			Unit targetImpedingMissle = TargetImpedingMissile(board, turn.actor.tile, pos);
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


	// TODO: This takes in a lot of repeated logic from the AwarenessController. Is there some way to share it?
	Unit TargetImpedingMissile(Board board, Tile missileSource, Point end)
	{
		Point pos = missileSource.pos;

		// A line algorithm borrowed from Rosetta Code http://rosettacode.org/wiki/Bitmap/Bresenham%27s_line_algorithm#C.23
		int dx = Mathf.Abs(end.x - pos.x);
		int sx = pos.x < end.x ? 1 : -1;
		int dy = Mathf.Abs(end.y - pos.y);
		int sy = pos.y < end.y ? 1 : -1;
		int err = (dx > dy ? dx : -dy) / 2;
		int e2;
		Tile fromTile = null;
		for (; ; )
		{
			Tile tile = board.GetTile(pos);
			if (tile != null)
			{
				if (fromTile != null)
				{
					GameObject occupant = tile.occupant;
					if (occupant != null)
					{
						Unit newTarget = occupant.GetComponent<Unit>();
						if (newTarget != null)
							return newTarget;
					}
				}
				fromTile = tile;
			}
			e2 = err;
			if (e2 > -dx) { err -= dy; pos.x += sx; }
			if (e2 < dy) { err += dx; pos.y += sy; }
			if (pos == end) break;
		}
		return null;
	}
}