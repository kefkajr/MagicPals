﻿using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class PerformAbilityState : BattleState {
	public override void Enter() {
		base.Enter();
		owner.turnOrderController.DidActorPerformActionType(ActionType.Major);

		if (turn.hasUnitMoved)
			turn.lockMove = true;
		StartCoroutine(Animate());
	}
	
	IEnumerator Animate() {
		// TODO play animations, etc
		yield return null;
		ApplyAbility();
		HandleNoise();
		
		if (IsBattleOver())
			owner.ChangeState<CutSceneState>();
		else if (!UnitCanReceiveCommands())
			owner.ChangeState<SelectUnitState>();
		else if (turn.hasUnitMoved)
			owner.ChangeState<EndFacingState>();
		else
			owner.ChangeState<CommandSelectionState>();
	}
	
	void ApplyAbility() {
		Console.Main.Log(string.Format("{0} uses {1}", turn.actor.name, turn.ability.name));
		turn.ability.Perform(turn.targets);
	}

	void HandleNoise() {
		if (turn.ability.noisy != null) {
			// Find noisy tiles and find out if any units can hear them
			List<Tile> noisyTiles = owner.board.Search(turn.actor.tile, NoiseExpandSearch);
			List<Unit> units = new List<Unit>(owner.units); ;
			units.Remove(owner.turn.actor);
			foreach (Unit unit in units) {
				owner.awarenessController.Listen(unit, owner.turn.abilityEpicenterTile.pos, noisyTiles, turn.actor.stealth);
			}
		}
    }

	bool NoiseExpandSearch(Tile from, Tile to) {
		// Height isn't being handled right now for noise perception.
		return (from.distance + 1) <= turn.ability.noisy.radius;
	}
}