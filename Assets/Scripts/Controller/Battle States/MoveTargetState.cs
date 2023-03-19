using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveTargetState : BattleState
{
	List<Tile> tiles;
	
	public override void Enter ()
	{
		base.Enter ();
		Movement mover = turn.actor.GetComponent<Movement>();
		tiles = mover.GetTilesInRange(board);
		board.SelectTiles(tiles);
		RefreshPrimaryStatPanel(pos);
		if (driver.Current == DriverType.Computer)
			StartCoroutine(ComputerHighlightMoveTarget());
	}
	
	public override void Exit ()
	{
		base.Exit ();
		board.DeSelectTiles(tiles);
		tiles = null;
		statPanelController.HidePrimary();
	}

	protected override void OnMove(object sender, MoveEventData moveEventData)
	{
		SelectTile(moveEventData.pointTranslatedByCameraDirection + pos);
		RefreshPrimaryStatPanel(pos);
	}
	
	protected override void OnSubmit ()
	{
		if (tiles.Contains(owner.currentTile))
			owner.ChangeState<MoveSequenceState>();
	}

	protected override void OnCancel() {
		owner.ChangeState<CommandSelectionState>();
	}

	IEnumerator ComputerHighlightMoveTarget ()
	{
		// Skip to MoveSequenceState if the plan of attack has not move location
		if (turn.plan.moveLocation == null)
			turn.plan.moveLocation = turn.actor.tile;

		Point cursorPos = pos;
		while (cursorPos != turn.plan.moveLocation.pos)
		{
			if (cursorPos.x < turn.plan.moveLocation.pos.x) cursorPos.x++;
			if (cursorPos.x > turn.plan.moveLocation.pos.x) cursorPos.x--;
			if (cursorPos.y < turn.plan.moveLocation.pos.y) cursorPos.y++;
			if (cursorPos.y > turn.plan.moveLocation.pos.y) cursorPos.y--;
			SelectTile(cursorPos);
			yield return new WaitForSeconds(0.25f);
		}
		yield return new WaitForSeconds(0.5f);
		owner.ChangeState<MoveSequenceState>();
		owner.board.DeSelectTiles(new List<Tile>(owner.board.tiles.Values)); // TODO remove this when done debugging pathfinding
	}
}