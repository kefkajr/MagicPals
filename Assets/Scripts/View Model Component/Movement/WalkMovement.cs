using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class WalkMovement : Movement 
{
	#region Protected
	protected override bool ExpandSearch (Board board, Tile from, Tile to)
	{
		// Skip if the distance in height between the two tiles is more than the unit can jump
		if ((Mathf.Abs(from.height - to.height) > jumpHeight))
			return false;

		// Skip if the tile is occupied by an enemy
		if (to.occupant != null)
			return false;

		// Skip if walls are blocking the way
		if (board.DoesWallSeparateTiles(from, to))
			return false;

		return base.ExpandSearch(board, from, to);
	}
	
	public override IEnumerator Traverse (Board board, Tile tile, MoveSequenceState moveSequenceState)
	{
		// Build a list of way points from the unit's 
		// starting tile to the destination tile
		List<Tile> targets = new List<Tile>();
		while (tile != null)
		{
			targets.Insert(0, tile);
			tile = tile.prev;
		}

		// Move to each way point in succession
		for (int i = 1; i < targets.Count; ++i)
		{
			Tile from = targets[i-1];
			Tile to = targets[i];

			Direction dir = from.GetDirection(to);
			if (unit.dir != dir)
				yield return StartCoroutine(Turn(dir));

			// Check if this unit spotted another unit
			List<Awareness> spottedAwarenesses = moveSequenceState.Spot();
			if (spottedAwarenesses.Count > 0)
            {
				// Pause and then trigger emergency turn
				yield return new WaitForSeconds(1);
				moveSequenceState.TriggerEmergencyTurn(spottedAwarenesses[0].stealth.unit);
				yield break;
			}

			// Walk
			if (from.height == to.height)
				yield return StartCoroutine(Walk(to));
			else
				yield return StartCoroutine(Jump(to));

			unit.Place(to);

			// Check if this unit was spotted by another unit
			if (moveSequenceState.DidGetSpottedByUnits())
            {
				// TODO: Figure out a good oportunity to stop the player during their own turn.

				if (false)
				{
					// Pause and then trigger emergency turn
					yield return new WaitForSeconds(1);
					moveSequenceState.TriggerEmergencyTurn(unit);
					yield break;
				}
			}

			if(to.trap != null)
            {
				// Run trap handler and end traversal
				moveSequenceState.TriggerTrap(to);
				yield break;
            }
		}

		yield return null;
	}
	#endregion

	#region Private
	IEnumerator Walk (Tile target)
	{
		Tweener tweener = transform.MoveTo(target.center, animationDuration, EasingEquations.Linear);
		while (tweener != null)
			yield return null;
	}

	IEnumerator Jump (Tile to)
	{
		Tweener tweener = transform.MoveTo(to.center, animationDuration, EasingEquations.Linear);

		Tweener t2 = jumper.MoveToLocal(new Vector3(0, Tile.stepHeight * 2f, 0), tweener.duration / 2f, EasingEquations.EaseOutQuad);
		t2.loopCount = 1;
		t2.loopType = EasingControl.LoopType.PingPong;

		while (tweener != null)
			yield return null;
	}
	#endregion
}