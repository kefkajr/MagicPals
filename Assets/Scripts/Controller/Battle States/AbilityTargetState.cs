using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AbilityTargetState : BattleState 
{
	List<Tile> tiles;
	AbilityRange ar;
	
	public override void Enter ()
	{
		base.Enter ();
		ar = turn.ability.GetComponent<AbilityRange>();
		SelectTiles ();
		statPanelController.ShowPrimary(turn.actor.gameObject);
		if (ar.directionOriented)
			RefreshSecondaryStatPanel(pos);
		if (driver.Current == DriverType.Computer)
			StartCoroutine(ComputerHighlightTarget());
	}
	
	public override void Exit ()
	{
		base.Exit ();
		board.DeSelectTiles(tiles);
		statPanelController.HidePrimary();
		statPanelController.HideSecondary();
	}

	protected override void OnMove(object sender, MoveEventData moveEventData)
	{
		if (ar.directionOriented)
		{
			ChangeDirection(moveEventData.pointTranslatedByCameraDirection);
		}
		else
		{
			SelectTile(moveEventData.pointTranslatedByCameraDirection + pos);
			RefreshSecondaryStatPanel(pos);
		}
	}
	
	protected override void OnFire (object sender, InfoEventArgs<int> e)
	{
		if (e.info == 0)
		{
			if (ar.directionOriented || tiles.Contains(board.GetTile(pos)))
			{
				turn.abilityEpicenterTile = board.GetTile(pos);
				owner.ChangeState<ConfirmAbilityTargetState>();
			}
		}
		else
		{
			// If the ability is inside of an item, return to the item list.
			// Otherwise, go to the category list.
			Merchandise merchandise = turn.ability.GetComponentInParent<Merchandise>();
			if (merchandise != null)
				owner.ChangeState<ItemOptionState>();
			else
				owner.ChangeState<CategorySelectionState>();
		}
	}
	
	void ChangeDirection (Point p)
	{
		Directions dir = p.GetDirection();
		if (turn.actor.dir != dir)
		{
			board.DeSelectTiles(tiles);
			turn.actor.dir = dir;
			turn.actor.Match();
			SelectTiles ();
		}
	}
	
	void SelectTiles ()
	{
		tiles = ar.GetTilesInRange(board);
		board.SelectTiles(tiles);
	}

	IEnumerator ComputerHighlightTarget ()
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

		// Define ability epicenter
		if (ar.directionOriented || tiles.Contains(board.GetTile(pos)))
		{
			turn.abilityEpicenterTile = board.GetTile(pos);
		}

		owner.ChangeState<ConfirmAbilityTargetState>();
	}
}