using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class FlyMovement : Movement  {
	public override IEnumerator Traverse(Board board, Tile tile, MoveSequenceState moveSequenceState) {
		// Store the distance between the start tile and target tile
		float dist = Mathf.Sqrt(Mathf.Pow(tile.pos.x - unit.tile.pos.x, 2) + Mathf.Pow(tile.pos.y - unit.tile.pos.y, 2));
		unit.Place(tile);

		// Fly high enough not to clip through any ground tiles
		float y = Tile.stepHeight * 10;
		float duration = (y - jumper.position.y) * animationDuration;
		Tweener tweener = jumper.MoveToLocal(new Vector3(0, y, 0), duration, EasingEquations.EaseInOutQuad);
		while (tweener != null)
			yield return null;

		// Turn to face the general direction
		Direction dir;
		Vector3 toTile = (tile.center - transform.position);
		if (Mathf.Abs(toTile.x) > Mathf.Abs(toTile.z))
			dir = toTile.x > 0 ? Direction.East : Direction.West;
		else
			dir = toTile.z > 0 ? Direction.North : Direction.South;
		yield return StartCoroutine(Turn(dir));

		// Move to the correct position
		duration = dist * animationDuration;
		tweener = transform.MoveTo(tile.center, duration, EasingEquations.EaseInOutQuad);
		while (tweener != null)
			yield return null;

		// Land
		duration = (y - tile.center.y) * animationDuration;
		tweener = jumper.MoveToLocal(Vector3.zero, duration, EasingEquations.EaseInOutQuad);
		while (tweener != null)
			yield return null;

		if (tile.trap != null) {
			// Run trap handler and end traversal
			moveSequenceState.TriggerTrap(tile);
			yield break;
		}
	}
}