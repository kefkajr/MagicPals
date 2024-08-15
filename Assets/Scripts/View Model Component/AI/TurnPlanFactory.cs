using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

public class TurnPlanFactory {
    ComputerPlayer cpu;

    BattleController BC { get { return cpu.BC; }}
    AwarenessController AC { get { return BC.awarenessController; } }
    Unit actor { get { return cpu.BC.turn.actor; }}
    Alliance actorAlliance { get { return actor.GetComponent<Alliance>(); }}

    public TurnPlanFactory(ComputerPlayer cpu) {
        this.cpu = cpu;
    }

    public TurnPlan EvaluateStrategy(Strategy strategy) {
		List<PlanScratchPad> planScratchPads = new();
		for (int i = 0; i < strategy.objectiveTypes.Count; ++i) {
			ObjectiveType objectiveType = strategy.objectiveTypes[i];
			for (int ii = 0; ii < strategy.gambits.Count; ++ii) {
				Gambit gambit = strategy.gambits[i];
				if (!gambit.IsViable(BC)) {
					continue;
				}
				Debug.Log("Evaluating gambit " + gambit.name + " with objective " + objectiveType.ToString());
				Strategem strategem = new Strategem(gambit, objectiveType);

				// Determine where to move and aim to best use the ability
				AbilityRange range = gambit.ability.GetComponent<AbilityRange>();
				if (range.positionOriented == false)
					// It doesn't matter where you stand
					planScratchPads.AddRange(
						PlanPositionIndependent(strategem)
					);
				else if (!range.directionOriented)
					// It DOES matter where you stand, but it doesn't matter where you face
					planScratchPads.AddRange(
						PlanDirectionIndependent(strategem)
					);
				else
					// It DOES matter where you stand and it DOES matter where you face
					planScratchPads.AddRange(
						PlanDirectionDependent(strategem)
					);
			}
		}

		Debug.Log("planScratchPads.Count: " + planScratchPads.Count);
		PlanScratchPad bestPlanScratchPad = PickBestPlanScratchPad(planScratchPads);

		if (bestPlanScratchPad == null) return null;

		TurnPlan plan = new(bestPlanScratchPad.strategem.gambit) {
			fireLocation = bestPlanScratchPad.abilityTargetTile,
			attackDirection = bestPlanScratchPad.direction,
			moveLocation = FindNearestMoveOptionToTile(bestPlanScratchPad.bestMoveTile,
				objectiveType: bestPlanScratchPad.strategem.objectiveType)
		};

		return plan;
	}

	/* When it is determined that an ability is position independent,
	 * then I simply move to a random tile within the unit’s move range, because position didn’t matter.
	 * There is room to polish this up –
	 * perhaps the unit would rather move toward or away from its foes during this time.
	 * If you want more specific behavior like that it shouldn’t be hard to add.
	 * I’ll show an example of moving toward the nearest foe soon. */
	List<PlanScratchPad> PlanPositionIndependent(Strategem strategem) {
		List<Tile> moveOptions = cpu.GetMoveOptions(strategem.objectiveType);
		// TODO: Have the unit move somewhere logical, instead of moving randomly
		Tile tile = moveOptions[Random.Range(0, moveOptions.Count - 1)];
		Debug.Log("moveOptions.Count is " + moveOptions.Count + ". Randomly moving to " + tile);
		TurnPlan plan = new(strategem.gambit);
		plan.moveLocation = plan.fireLocation = tile;
		PlanScratchPad planScratchPad = new PlanScratchPad(strategem);
		planScratchPad.AddMoveTarget(tile);
		return new List<PlanScratchPad>{planScratchPad};
	}

	/* The next case is where the position matters, but the facing angle does not.
	 * For example, casting a spell such as “Fire” or “Cure” can target different units
	 * based on where you move the aiming cursor.
	 * Because you can move before firing,
	 * a unit can actually reach targets in a larger radius than just the range of the ability by itself.
	 * Because of this I iterate through a nested loop,
	 * where an outer loop considers every possible position a unit can move to,
	 * and an inner loop considers every tile within firing range of that move location.
	 * 
	 * Remember that even after considering movement range and ability range,
	 * we still have an area of effect on the ability itself.
	 * This would need to be considered next.
	 * However, there are likely to be a lot of “overlapping” entries here.
	 * For example, whether I move one space to the left or one space to the right,
	 * I can still fire one space in front of the original location with most ranged abilities.
	 * Therefore, I added a dictionary which mapped from a selected tile to an object
	 * which records notes on that location such as which targets fall within range of the area of effect.
	 * I only create and evaluate this note object the first time I determine that a tile is within firing range.
	 * Otherwise, I simply refer to the notes I had already taken and indicate that
	 * another tile is also a valid place to fire from.
	 * 
	 * Before I start going through the loops I recorded the tile the actor was originally placed on.
	 * Before the method exits, I move the unit back to the original position.
	 * It’s important not to forget this step or the game would be out of sync with the visuals
	 * in the game every time the AI took a turn.
	 * 
	 * Finally, I pass the list of action scratch pad we have built up to this point to a method
	 * which can pick the best overall action for our turn. */
	List<PlanScratchPad> PlanDirectionIndependent(Strategem strategem) {
		Tile startTile = actor.tile;
		Dictionary<Tile, PlanScratchPad> planScratchPadByTile = new Dictionary<Tile, PlanScratchPad>();
		AbilityRange ar = strategem.gambit.ability.GetComponent<AbilityRange>();
		List<Tile> moveOptions = cpu.GetMoveOptions(strategem.objectiveType);
		
		for (int i = 0; i < moveOptions.Count; ++i) {
			Tile moveTile = moveOptions[i];
			actor.Place( moveTile );
			List<Tile> abilityTargetOptions = ar.GetTilesInRange(BC.board).OrderBy(tile => tile.pos.x).ThenBy(tile => tile.pos.y).ToList();;
			for (int j = 0; j < abilityTargetOptions.Count; ++j) {
				Tile abilityTargetOption = abilityTargetOptions[j];
				// if (abilityTargetOption.ToString() == "Tile: (4,1)") {
				// 	print("This is it.");
				// }
				PlanScratchPad planScratchPad;
				if (planScratchPadByTile.ContainsKey(abilityTargetOption)) {
					planScratchPad = planScratchPadByTile[abilityTargetOption];
				} else {
                    planScratchPad = new PlanScratchPad(strategem);
					planScratchPad.abilityTargetTile = abilityTargetOption;
					planScratchPad.direction = actor.dir;
					planScratchPad = RateFireLocation(strategem.gambit, planScratchPad);
					planScratchPadByTile[abilityTargetOption] = planScratchPad;
				}
				// Include the original move option as a target for this PlanScratchPad
				planScratchPad.AddMoveTarget(moveTile);
			}
		}
		
		actor.Place(startTile);
		List<PlanScratchPad> planScratchPads = new List<PlanScratchPad>(planScratchPadByTile.Values);
		return planScratchPads;
	}

	/* This last case depends both on a unit’s position on the board
	 * and the direction the unit faces while using the selected ability.
	 * It should look pretty similar to the “PlanDirectionIndependent” variation.
	 * The main difference here is that instead of grabbing the Ability Range component
	 * and looping through targeted tiles, we instead loop through each of the four facing directions.
	 * Every single entry generated will have a unique area of effect –
	 * there is no overlap or need for the dictionary as I had last time.
	 * We can simply track each entry in a list directly. */
	List<PlanScratchPad> PlanDirectionDependent(Strategem strategem) {
		Tile startTile = actor.tile;
		Direction startDirection = actor.dir;
		List<PlanScratchPad> planScratchPads = new List<PlanScratchPad>();
		List<Tile> moveOptions = cpu.GetMoveOptions(strategem.objectiveType);
		
		for (int i = 0; i < moveOptions.Count; ++i) {
			Tile moveTile = moveOptions[i];
			actor.Place( moveTile );
			
			for (int ii = 0; ii < 4; ++ii) {
				actor.dir = (Direction)ii;
                PlanScratchPad planScratchPad = new PlanScratchPad(strategem) {
                    abilityTargetTile = moveTile,
                    direction = actor.dir
                };
                planScratchPad = RateFireLocation(strategem.gambit, planScratchPad);
				planScratchPad.AddMoveTarget(moveTile);
				planScratchPads.Add(planScratchPad);
			}
		}
		
		actor.Place(startTile);
		actor.dir = startDirection;
		return planScratchPads;
	}

	/* As we were creating each PlanScratchPad (a note on the effect area of using an ability),
	 * we needed a way to rate it, so we could sort them later and pick the best one.
	 * We accomplish this by looping through the area that the ability could reach from a given firing location.
	 * Any tile which is a “legal” target for an ability gets a “mark” –
	 * for example you can “Attack” any unit whether friend or foe,
	 * so any tile with a unit would be marked by the attack ability.
	 * However, we also indicate whether or not the tile is determined to be a “match”
	 * (the desired target type for the given ability).
	 * In the example before, any given unit would consider a tile with an ally is not a match,
	 * but tiles with a foe are a match for the attack ability.
	 * 
	 * By tracking all of the marks, but also specifying which ones are matches or not,
	 * we can better rate a move.
	 * For example, if my attack would hit exactly one foe and one ally by targeting tile ‘X’,
	 * and exactly one foe but no allies by targeting tile ‘Y’,
	 * then the second option is better.
	 * I can tally up a score such that marks which are matches incremenet the score
	 * and marks that are not a match decrement the score.
	 * 
	 * Note that I intentially skip the tile on which the caster is currently standing,
	 * because that may not be the unit’s location when it moves before firing.
	 * We will need to adjust scores based on the caster’s location at a later point. */
	PlanScratchPad RateFireLocation (Gambit gambit, PlanScratchPad planScratchPad) {
		AbilityArea area = gambit.ability.GetComponent<AbilityArea>();
		List<Tile> tiles = area.GetTilesInArea(BC.board, planScratchPad.abilityTargetTile.pos);
		planScratchPad.areaTargets = tiles;
		planScratchPad.isCasterMatch = IsAbilityTargetMatch(gambit.targetType, actor.tile);

		for (int i = 0; i < tiles.Count; ++i) {
			Tile tile = tiles[i];
			if (actor.tile == tiles[i] || !gambit.ability.IsTarget(tile))
				continue;
			
			bool isMatch = IsAbilityTargetMatch(gambit.targetType, tile);
			planScratchPad.AddMark(tile, isMatch);
		}
		return planScratchPad;
	}

	/* This method shows how to determine which marks are a match or not.
	 * An ability which targets a tile is simply marked as true
	 * (I havent actually implemented any such abilities, so I might change this logic later).
	 * Otherwise, I use the alliance component to determine whether or not the target type is a match. */
	bool IsAbilityTargetMatch(TargetType targetType, Tile tile) {
		bool isMatch = false;
		if (targetType == TargetType.Tile)
			isMatch = true;
		else if (targetType != TargetType.None) {
			Alliance targetAlliance = tile.occupant.GetComponentInChildren<Alliance>();
			Unit targetUnit = targetAlliance.GetComponent<Unit>();
			if (targetAlliance != null) {
				if (actorAlliance.IsMatch(targetAlliance, targetType)) {
					if (actorAlliance == targetAlliance &&
					  (targetType == TargetType.Self || targetType == TargetType.AllyOrSelf)) {
						isMatch = true;
					} else if (targetType == TargetType.Ally) {
						// Allies are assumed to have automatic knowledge of each other's location
						isMatch = true;
					} else {
						// If the target is a foe who has been seen by the actor, it's viable
						isMatch = AC.IsAwareOfUnit(actor, targetUnit, new AwarenessType[] {AwarenessType.Seen});
					}
				}

			}
		}

		return isMatch;
	}

	/* This is the method that actually provides a score for each of the PlanScratchPads.
	 * It goes through two “passes” of analyzing our PlanScratchPads.
	 * On the first pass, it scores each PlanScratchPad based on having
	 * more marks which are matches than marks which are not matches.
	 * 
	 * Whenever I find a new “best” score, I track what the score was,
	 * and add the ability to a list of the PlanScratchPads which I consider to be the best.
	 * This list will be cleared if I should find a better score,
	 * but if I find additional PlanScratchPads with a tied score then I will also add them to the list.
	 * 
	 * When all of the PlanScratchPads have been scored, it is actually possible
	 * that I wont have any entries in my best PlanScratchPads list.
	 * This would be the case where an ability could technically be used,
	 * but the effect would be detrimental to the user’s party.
	 * For example, if the only PlanScratchPad an AI unit had to attack was one of its allies,
	 * then it would be better not to do anything than to actually perform the ability.
	 * In these cases, I mark the plan’s abilty as null so that it wont be performed.
	 * 
	 * In the cases where I do have some beneficial PlanScratchPads to pick from,
	 * I will then run another pass to help trim down the PlanScratchPads even further.
	 * There are multiple reasons for this. For example,
	 * lets say I can attack a target unit from multiple different move locations.
	 * Some of those locations may be from the front, while others may be from the back.
	 * If I can pick, I would want to pick an angle from the back
	 * so that my chances of the attack hitting are greater.
	 * 
	 * By the end of this second “pass” I should have one or more PlanScratchPads which were added to the final picks.
	 * Because they all share the same score,
	 * I pick any of them at random and assign the relevant details to our turn plan. */
	PlanScratchPad PickBestPlanScratchPad(List<PlanScratchPad> planScratchPads) {

		int bestScore = 1;
		List<PlanScratchPad> bestPlanScratchPads = new List<PlanScratchPad>();
		for (int i = 0; i < planScratchPads.Count; ++i) {
			PlanScratchPad planScratchPad = planScratchPads[i];
			int score = planScratchPad.GetScore(actor, planScratchPad.strategem.gambit.ability);
			
			if (score > bestScore) {
				bestScore = score;
				bestPlanScratchPads.Clear();
				bestPlanScratchPads.Add(planScratchPad);
			} else if (score == bestScore) {
				bestPlanScratchPads.Add(planScratchPad);
			}
		}

		if (bestPlanScratchPads.Count == 0) {
			Debug.Log("Could not find a best plan scratch pad.");
			return null;
		}

		List<PlanScratchPad> finalPicks = new List<PlanScratchPad>();
		bestScore = 0;
		for (int i = 0; i < bestPlanScratchPads.Count; ++i) {
			PlanScratchPad planScratchPad = bestPlanScratchPads[i];
			int score = planScratchPad.bestAngleBasedScore;
			if (score > bestScore) {
				bestScore = score;
				finalPicks.Clear();
				finalPicks.Add(planScratchPad);
			} else if (score == bestScore) {
				finalPicks.Add(planScratchPad);
			}
		}

		if (finalPicks.Count > 0) {
			Debug.Log("Final picks count: " + finalPicks.Count);
			for (int i = 0; i < finalPicks.Count; ++i) {
				PlanScratchPad planScratchPad = bestPlanScratchPads[i];
				Debug.Log("Ability target tile: " + planScratchPad.abilityTargetTile + ", best move tile: " + planScratchPad.bestMoveTile);
			}
		}
		PlanScratchPad choice = finalPicks[ Random.Range(0, finalPicks.Count)];
		Debug.Log("FINAL CHOICE: Ability target tile: " + choice.abilityTargetTile + ", best move tile: " + choice.bestMoveTile);
		return choice;
	}

	Tile FindNearestMoveOptionToTile(Tile tile, ObjectiveType objectiveType) {
		var moveOptions = cpu.GetMoveOptions(objectiveType);
		Tile destination = null;
		if (moveOptions.Contains(tile)) {
			return tile;
		} else {
			BC.board.FindPath(BC.turn.actor, BC.turn.actor.tile, BC.board.GetTile(destination.pos), delegate (List<Tile> finalPath) {
				Tile toCheck = tile;
				while (toCheck != null) {
					if (moveOptions.Contains(toCheck)) {
						// Move toward top awareness / point of interest
						destination = toCheck;
					}
					// Board search keeps previous tiles in memory
					toCheck = toCheck.prev;
				}
			});
		}
		return destination;
	}
}