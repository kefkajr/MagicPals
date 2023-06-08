using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class TeleportMovement : Movement {
	public override IEnumerator Traverse(Board board, Tile tile, MoveSequenceState moveSequenceState) {
		unit.Place(tile);

		Tweener spin = jumper.RotateToLocal(new Vector3(0, 360, 0), animationDuration, EasingEquations.EaseInOutQuad);
		spin.loopCount = 1;
		spin.loopType = EasingControl.LoopType.PingPong;

		Tweener shrink = transform.ScaleTo(Vector3.zero, animationDuration, EasingEquations.EaseInBack);

		while (shrink != null)
			yield return null;

		transform.position = tile.center;

		Tweener grow = transform.ScaleTo(Vector3.one, animationDuration, EasingEquations.EaseOutBack);
		while (grow != null)
			yield return null;

		if (tile.trap != null) {
			// Run trap handler and end traversal
			moveSequenceState.TriggerTrap(tile);
			yield break;
		}
	}
}