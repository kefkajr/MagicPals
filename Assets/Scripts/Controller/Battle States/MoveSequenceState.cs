using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

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
			TriggerTrap(turn.actor.tile);
			yield return null;
		}

		Movement m = turn.actor.GetComponent<Movement>();
		yield return StartCoroutine(m.Traverse(board: board, tile: owner.currentTile, moveSequenceState: this));

		turn.hasUnitMoved = true;

		Time.timeScale = 1f;
		owner.ChangeState<CommandSelectionState>();
	}

    public void TriggerTrap(Tile tile)
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

		Time.timeScale = 1f;
		// Perform trap ability
		owner.ChangeState<PerformAbilityState>();
	}

	public List<Awareness> Spot()
    {
		// If the driver is Human, we don't want to give units they look at an Emergency Turn
		DriverType currentDriverType = owner.turn.actor.GetComponent<Driver>().Current;

		if (currentDriverType == DriverType.Human)
			return new List<Awareness>();

		List<Awareness> awarenesses = owner.awarenessController.Look(owner.turn.actor).Where((a) => a.type == AwarenessType.Seen).ToList();
		List<Awareness> playerAwarenesses = awarenesses.Where((a) => a.stealth.GetComponent<Driver>().Current == DriverType.Human).ToList();
		// TODO: What about when multiple playable characters are spotted?
		return playerAwarenesses;
	}

	public bool DidGetSpottedByUnits()
	{
		// If the driver is Human, and they get spotted, then they should get an Emergency Turn
		DriverType currentDriverType = owner.turn.actor.GetComponent<Driver>().Current;

		if (currentDriverType != DriverType.Human)
			return false;

		List<Awareness> totalAwarenesses = new List<Awareness>();
		List<Unit> computerUnits = owner.units.Where(u => u.GetComponent<Driver>().Current == DriverType.Computer).ToList();
		foreach (Unit unit in computerUnits)
		{
			List<Awareness> awarenesses = owner.awarenessController.Look(unit).Where((a) => a.type == AwarenessType.Seen).ToList();
			totalAwarenesses.AddRange(awarenesses);
		}

		List<Awareness> playerAwarenesses = totalAwarenesses.Where((a) => a.stealth.GetComponent<Driver>().Current == DriverType.Human).ToList();
		if (playerAwarenesses.Count > 0)
		{
			return true;
		}

		return false;
	}

	public void TriggerEmergencyTurn(Unit unit)
    {
		owner.awarenessController.InitiateEmergencyTurn(unit);
	}

	protected override void OnSubmit()
	{
		Time.timeScale = 10f;
	}
}