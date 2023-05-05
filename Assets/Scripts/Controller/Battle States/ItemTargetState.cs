using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemTargetState : BattleState
{
	List<Tile> tiles;
	AbilityRange ar;

	public override void Enter()
	{
		base.Enter();
		ar = turn.ability.GetComponent<AbilityRange>();
		SelectTiles();
		statPanelController.ShowPrimary(turn.actor.gameObject);
		if (ar.directionOriented)
			RefreshSecondaryStatPanel(pos);
		if (driver.Current == DriverType.Computer)
			StartCoroutine(ComputerHighlightTarget());
	}

	public override void Exit()
	{
		base.Exit();
		board.DeHighlightTiles(tiles);
		statPanelController.HidePrimary();
		statPanelController.HideSecondary();
	}

	protected override void OnMove(object sender, MoveEventData d)
	{
		if (ar.directionOriented)
		{
			ChangeDirection(d.pointTranslatedByCameraDirection);
		}
		else
		{
			SelectTile(d.pointTranslatedByCameraDirection + pos);
			RefreshSecondaryStatPanel(pos);
		}
	}

	protected override void OnSubmit()
	{
		if (ar.directionOriented || tiles.Contains(board.GetTile(pos)))
			owner.ChangeState<ConfirmAbilityTargetState>();			
	}

	protected override void OnCancel() {
		owner.ChangeState<CategorySelectionState>();
	}

	void ChangeDirection(Point p)
	{
		Direction dir = p.GetDirection();
		if (turn.actor.dir != dir)
		{
			board.DeHighlightTiles(tiles);
			turn.actor.dir = dir;
			turn.actor.Match();
			SelectTiles();
		}
	}

	void SelectTiles()
	{
		tiles = ar.GetTilesInRange(board);
		board.HighlightTiles(tiles, BoardColorType.targetRangeHighlight);
	}

	IEnumerator ComputerHighlightTarget()
	{
		if (ar.directionOriented)
		{
			ChangeDirection(turn.plan.attackDirection.GetNormal());
			yield return new WaitForSeconds(0.25f);
		}
		else
		{
			Point cursorPos = pos;
			while (cursorPos != turn.plan.fireLocation.pos)
			{
				if (cursorPos.x < turn.plan.fireLocation.pos.x) cursorPos.x++;
				if (cursorPos.x > turn.plan.fireLocation.pos.x) cursorPos.x--;
				if (cursorPos.y < turn.plan.fireLocation.pos.y) cursorPos.y++;
				if (cursorPos.y > turn.plan.fireLocation.pos.y) cursorPos.y--;
				SelectTile(cursorPos);
				yield return new WaitForSeconds(0.25f);
			}
		}
		yield return new WaitForSeconds(0.5f);
		owner.ChangeState<ConfirmAbilityTargetState>();
	}
}