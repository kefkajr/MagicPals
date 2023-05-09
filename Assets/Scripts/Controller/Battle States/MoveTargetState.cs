using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MoveTargetState : BattleState
{
	List<Tile> tiles;
	
	public override void Enter ()
	{
		base.Enter ();
		Movement mover = turn.actor.GetComponent<Movement>();
		tiles = mover.GetTilesInRange(board);
		board.HighlightTiles(tiles, BoardColorType.moveRangeHighlight);
		RefreshPrimaryStatPanel(pos);
		if (driver.Current == DriverType.Computer)
			StartCoroutine(ComputerMoveTarget());
	}
	
	public override void Exit ()
	{
		base.Exit ();
		board.DeHighlightTiles(tiles);
		tiles = null;
		statPanelController.HidePrimary();
	}

	protected override void OnMove(object sender, MoveEventData moveEventData)
	{
		base.OnMove(sender, moveEventData);
		// Keep move range tile highlights from being overridden by other highlights (i.e. viewing range highlights)
		board.HighlightTiles(tiles, BoardColorType.moveRangeHighlight);
	}

	protected override void OnSubmit ()
	{
		if (driver.Current == DriverType.Computer) return;
		
		if (tiles.Contains(owner.currentTile))
			owner.ChangeState<MoveSequenceState>();
	}

	protected override void OnCancel() {
		owner.ChangeState<CommandSelectionState>();
	}

	IEnumerator ComputerMoveTarget() {
		// In case the move location is outside of the unit's movement range,
		// find the nearest accessible tile still in the pathfinding memory
		Tile destination = turn.plan.moveLocation;
		if (destination == null)
			destination = turn.actor.tile;

		Point cursorPos = pos;
		while (cursorPos != destination.pos)
		{
			if (cursorPos.x < destination.pos.x) cursorPos.x++;
			if (cursorPos.x > destination.pos.x) cursorPos.x--;
			if (cursorPos.y < destination.pos.y) cursorPos.y++;
			if (cursorPos.y > destination.pos.y) cursorPos.y--;
			SelectTile(cursorPos);
			yield return new WaitForSeconds(0.25f);
		}
		yield return new WaitForSeconds(0.5f);
		owner.ChangeState<MoveSequenceState>();
		owner.board.DeHighlightTiles(new List<Tile>(owner.board.tiles.Values)); // TODO remove this when done debugging pathfinding
	}
}