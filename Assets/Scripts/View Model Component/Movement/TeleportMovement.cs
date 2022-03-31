using UnityEngine;
using System;
using System.Collections;

public class TeleportMovement : Movement 
{
	public override IEnumerator Traverse(Tile tile, Action<Tile> TrapHandler)
	{
		unit.Place(tile);

		Tweener spin = jumper.RotateToLocal(new Vector3(0, 360, 0), 0.5f, EasingEquations.EaseInOutQuad);
		spin.loopCount = 1;
		spin.loopType = EasingControl.LoopType.PingPong;

		Tweener shrink = transform.ScaleTo(Vector3.zero, 0.5f, EasingEquations.EaseInBack);

		while (shrink != null)
			yield return null;

		transform.position = tile.center;

		Tweener grow = transform.ScaleTo(Vector3.one, 0.5f, EasingEquations.EaseOutBack);
		while (grow != null)
			yield return null;

		// Run trap handler and end traversal
		TrapHandler(tile);
		yield break;
	}
}