using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MoveSequenceState : BattleState 
{
	public override void Enter ()
	{
		base.Enter ();
		StartCoroutine("Sequence");
	}
	
	IEnumerator Sequence ()
	{
		// Check if a trap is triggered in the starting tile
		if (turn.actor.tile.trap != null)
		{
			DidFindTrap(turn.actor.tile);
			yield return null;
		}

		Movement m = turn.actor.GetComponent<Movement>();
		yield return StartCoroutine(m.Traverse(board: board, tile: owner.currentTile, TrapHandler: DidFindTrap, AwarenessHandler: DidPerceiveNewStealths));

		turn.hasUnitMoved = true;

		Time.timeScale = 1f;

		owner.ChangeState<CommandSelectionState>();
	}

    void DidFindTrap(Tile tile)
    {
		// Set trap and ability
		Trap trap = tile.trap;
		turn.ability = trap.GetComponent<Ability>();
		Debug.Log(turn.ability.name + " Trap triggered!");

		// Halt unit movement
		turn.actor.Place(tile);
		turn.hasUnitMoved = true;
		SelectTile(tile.pos);

		// Define targets for trap ability
		AbilityArea aa = turn.ability.GetComponent<AbilityArea>();
		List<Tile> tiles = aa.GetTilesInArea(board, pos);
		turn.targets = new List<Tile>();
		for (int i = 0; i < tiles.Count; ++i)
			if (turn.ability.IsTarget(tiles[i]))
				turn.targets.Add(tiles[i]);

		// Remove trap from tile
		tile.trap = null;

		// Perform trap ability
		owner.ChangeState<PerformAbilityState>();
	}

	void DidPerceiveNewStealths(List<Awareness> newAwarenesses)
	{
		foreach (Awareness awareness in newAwarenesses)
		{
			Debug.Log(awareness.ToString());
		}
	}

	protected override void OnFire(object sender, InfoEventArgs<int> e)
	{
		Time.timeScale = 10f;
	}
}