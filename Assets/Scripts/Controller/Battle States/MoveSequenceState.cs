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
		Console.Main.Log(turn.ability.name + " Trap triggered!");

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

	bool DidPerceiveNewStealths(bool shouldCheckAllUnits)
	{
		List<Unit> units = shouldCheckAllUnits ? owner.units : new List<Unit>{ owner.turn.actor };
		List<Awareness> newAwarenesses = new List<Awareness>();
		foreach (Unit unit in units)
		{
			Perception perception = unit.GetComponent<Perception>();
			List<Awareness> awarenesses = perception.Look(board: board);
			newAwarenesses.AddRange(awarenesses);
		}

		if (newAwarenesses.Count > 0)
		{
			return true;
		}

		return false;
	}

	protected override void OnFire(object sender, InfoEventArgs<int> e)
	{
		Time.timeScale = 10f;
	}
}