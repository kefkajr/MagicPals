using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AbilityTargetState : BattleState {
	List<Tile> tiles;
	AbilityRange range;
	AbilityArea area;
	
	public override void Enter() {
		base.Enter();
		range = turn.ability.GetComponent<AbilityRange>();
		area = turn.ability.GetComponent<AbilityArea>();

		HighlightRangeTiles ();
		HighlightAreaTiles();

		statPanelController.ShowPrimary(turn.actor.gameObject);
		if (range.directionOriented)
			RefreshSecondaryStatPanel(pos);
		if (driver.Current == DriverType.Computer)
			StartCoroutine(ComputerHighlightTarget());
	}
	
	public override void Exit() {
		base.Exit();
		board.DeHighlightTiles(tiles);
		statPanelController.HidePrimary();
		statPanelController.HideSecondary();
	}

	protected override void OnMove(object sender, MoveEventData moveEventData) {
		board.DeHighlightAllTiles();
		HighlightRangeTiles ();

		if (range.directionOriented) {
			ChangeDirection(moveEventData.pointTranslatedByCameraDirection);
		} else {
			SelectTile(moveEventData.pointTranslatedByCameraDirection + pos);
			RefreshSecondaryStatPanel(pos);
		}

		HighlightAreaTiles();
	}
	
	protected override void OnSubmit() {
		if (driver.Current == DriverType.Computer) return;

		if (range.directionOriented || tiles.Contains(board.GetTile(pos))) {
			turn.abilityEpicenterTile = board.GetTile(pos);
			owner.ChangeState<ConfirmAbilityTargetState>();
		}
	}

	protected override void OnCancel() {
		// If the ability is inside of an item, return to the item list.
		// Otherwise, go to the category list.
		Merchandise merchandise = turn.ability.GetComponentInParent<Merchandise>();
		if (merchandise != null)
			owner.ChangeState<ItemOptionState>();
		else
			owner.ChangeState<CategorySelectionState>();
	}

	// protected override void SelectTile (Point p) {
	// 	Tile potentialTile = board.GetTile(p);
	// 	if (tiles.Contains(potentialTile))
	// 		base.SelectTile(p);
	// }
	
	void ChangeDirection (Point p) {
		Direction dir = p.GetDirection();
		if (turn.actor.dir != dir) {
			board.DeHighlightTiles(tiles);
			turn.actor.dir = dir;
			turn.actor.Match();
		}
	}
	
	void HighlightRangeTiles () {
		tiles = range.GetTilesInRange(board);
		board.HighlightTiles(tiles, TileHighlightColorType.targetRangeHighlight);
	}

	void HighlightAreaTiles () {
		if (tiles.Contains(board.GetTile(pos))) {
			List<Tile> areaTiles = area.GetTilesInArea(board, pos);
			board.HighlightTiles(areaTiles, TileHighlightColorType.targetAreaHighlight);
		}
	}

	IEnumerator ComputerHighlightTarget () {
		if (range.directionOriented) {
			ChangeDirection(turn.plan.attackDirection.GetNormal());
			yield return new WaitForSeconds(0.25f);
		} else {
			Point cursorPos = pos;
			while (cursorPos != turn.plan.fireLocation.pos) {
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
		if (range.directionOriented || tiles.Contains(board.GetTile(pos))) {
			turn.abilityEpicenterTile = board.GetTile(pos);
		}

		owner.ChangeState<ConfirmAbilityTargetState>();
	}
}