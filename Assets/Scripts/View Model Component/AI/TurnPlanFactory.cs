using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

public class TurnPlanFactory {
    ComputerPlayer cpu;

    BattleController BC { get { return cpu.BC; }}
    Unit actor { get { return cpu.BC.turn.actor; }}

    public TurnPlanFactory(ComputerPlayer cpu) {
        this.cpu = cpu;
    }

    public TurnPlan EvaluateStrategy(Strategy strategy) {
		List<TurnPlan> turnPlans = new();

		for (int i = 0; i < strategy.gambits.Count; ++i) {
			Gambit gambit = strategy.gambits[i];
			for (int ii = 0; ii < strategy.objectiveTypes.Count; ++ii) {
			ObjectiveType objectiveType = strategy.objectiveTypes[ii];
			if (!gambit.IsViable(BC)) {
				continue;
			}
			Debug.Log("Evaluating gambit " + gambit.name + " with " + objectiveType.ToString());
			Strategem strategem = new(gambit, objectiveType, strategy.gambits.Count - i + (strategy.objectiveTypes.Count - ii) );

			// Determine where to move and aim to best use the ability
			AbilityRange range = gambit.ability.GetComponent<AbilityRange>();
			if (range.positionOriented == false)
				// It doesn't matter where you stand
				turnPlans.AddRange(
					PlanPositionIndependent(strategem)
				);
			else if (!range.directionOriented)
				// It DOES matter where you stand, but it doesn't matter where you face
				turnPlans.AddRange(
					PlanDirectionIndependent(strategem)
				);
			else
				// It DOES matter where you stand and it DOES matter where you face
				turnPlans.AddRange(
					PlanDirectionDependent(strategem)
				);
			}
		}

		for (int i = 0; i < turnPlans.Count; ++i) {
			TurnPlan plan = turnPlans[i];
			plan.CalculateScore(BC, actor);
		}

		if (turnPlans.Count == 0) return null;
		
		List<TurnPlan> orderedPlans = turnPlans.OrderByDescending(plan => plan.score).ToList();
		int topScore = orderedPlans.First().score;
		
		List<TurnPlan> bestPlans = orderedPlans.Where(plan => plan.score == topScore).ToList();
		return bestPlans[Random.Range(0, bestPlans.Count)];
	}

	/* When it is determined that an ability is position independent,
	 * then I simply move to a random tile within the unit’s move range, because position didn’t matter.
	 * There is room to polish this up –
	 * perhaps the unit would rather move toward or away from its foes during this time.
	 * If you want more specific behavior like that it shouldn’t be hard to add.
	 * I’ll show an example of moving toward the nearest foe soon. */
	List<TurnPlan> PlanPositionIndependent(Strategem strategem) {
		List<TurnPlan> turnPlans = new ();
		List<Tile> moveOptions = cpu.GetMoveOptions();
		for (int i = 0; i < moveOptions.Count; ++i) {
			Tile moveTile = moveOptions[i];
			TurnPlan plan = new(strategem);
			plan.moveLocation = plan.fireLocation = moveTile;
			turnPlans.Add(plan);
		}
 		return turnPlans;
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
	List<TurnPlan> PlanDirectionIndependent(Strategem strategem) {
		Tile startTile = actor.tile;
		List<TurnPlan> turnPlans = new ();
		AbilityRange ar = strategem.gambit.ability.GetComponent<AbilityRange>();
		List<Tile> moveOptions = cpu.GetMoveOptions();
		
		for (int i = 0; i < moveOptions.Count; ++i) {
			Tile moveTile = moveOptions[i];
			actor.Place( moveTile );
			List<Tile> abilityTargetOptions = ar.GetTilesInRange(BC.board).OrderBy(tile => tile.pos.x).ThenBy(tile => tile.pos.y).ToList();;
			for (int j = 0; j < abilityTargetOptions.Count; ++j) {
				Tile abilityTargetOption = abilityTargetOptions[j];
                // if (abilityTargetOption.ToString() == "Tile: (4,1)") {
                // 	print("This is it.");
                // }
                TurnPlan turnPlan = new (strategem) {
					moveLocation = moveTile,
                    fireLocation = abilityTargetOption,
                    attackDirection = actor.dir
                };
				turnPlans.Add(turnPlan);
            }
		}
		
		actor.Place(startTile);
		return turnPlans;
	}

	/* This last case depends both on a unit’s position on the board
	 * and the direction the unit faces while using the selected ability.
	 * It should look pretty similar to the “PlanDirectionIndependent” variation.
	 * The main difference here is that instead of grabbing the Ability Range component
	 * and looping through targeted tiles, we instead loop through each of the four facing directions.
	 * Every single entry generated will have a unique area of effect –
	 * there is no overlap or need for the dictionary as I had last time.
	 * We can simply track each entry in a list directly. */
	List<TurnPlan> PlanDirectionDependent(Strategem strategem) {
		Tile startTile = actor.tile;
		Direction startDirection = actor.dir;
		List<TurnPlan> turnPlans = new ();
		List<Tile> moveOptions = cpu.GetMoveOptions();
		
		for (int i = 0; i < moveOptions.Count; ++i) {
			Tile moveTile = moveOptions[i];
			actor.Place( moveTile );
			
			for (int ii = 0; ii < 4; ++ii) {
				actor.dir = (Direction)ii;
                TurnPlan turnPlan = new TurnPlan(strategem) {
                    moveLocation = moveTile,
                    attackDirection = actor.dir
                };
				turnPlans.Add(turnPlan);
			}
		}
		
		actor.Place(startTile);
		actor.dir = startDirection;
		return turnPlans;
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
	// PlanScratchPad PickBestTurnPlan(List<TurnPlan> turnPlans) {

	// 	int bestScore = 1;
	// 	List<PlanScratchPad> bestPlanScratchPads = new List<PlanScratchPad>();
	// 	for (int i = 0; i < planScratchPads.Count; ++i) {
	// 		PlanScratchPad planScratchPad = planScratchPads[i];
	// 		int score = planScratchPad.CalculateScore(cpu, actor, planScratchPad.strategem.gambit.ability);
			
	// 		if (score > bestScore) {
	// 			bestScore = score;
	// 			bestPlanScratchPads.Clear();
	// 			bestPlanScratchPads.Add(planScratchPad);
	// 		} else if (score == bestScore) {
	// 			bestPlanScratchPads.Add(planScratchPad);
	// 		}
	// 	}

	// 	if (bestPlanScratchPads.Count == 0) {
	// 		Debug.Log("Could not find a best plan scratch pad.");
	// 		return null;
	// 	}

	// 	List<PlanScratchPad> finalPicks = new List<PlanScratchPad>();
	// 	bestScore = 0;
	// 	for (int i = 0; i < bestPlanScratchPads.Count; ++i) {
	// 		PlanScratchPad planScratchPad = bestPlanScratchPads[i];
	// 		int score = planScratchPad.bestAngleBasedScore;
	// 		if (score > bestScore) {
	// 			bestScore = score;
	// 			finalPicks.Clear();
	// 			finalPicks.Add(planScratchPad);
	// 		} else if (score == bestScore) {
	// 			finalPicks.Add(planScratchPad);
	// 		}
	// 	}

	// 	if (finalPicks.Count > 0) {
	// 		Debug.Log("Final picks count: " + finalPicks.Count);
	// 		for (int i = 0; i < finalPicks.Count; ++i) {

	// 			PlanScratchPad planScratchPad = bestPlanScratchPads[i];
	// 			Debug.Log("Ability target tile: " + planScratchPad.abilityTargetTile + ", best move tile: " + planScratchPad.bestMoveTile);
	// 		}
	// 	}
	// 	PlanScratchPad choice = finalPicks[ Random.Range(0, finalPicks.Count)];
	// 	Debug.Log("FINAL CHOICE: Ability target tile: " + choice.abilityTargetTile + ", best move tile: " + choice.bestMoveTile);
	// 	return choice;
	// }

	public List<TurnPlan> DebugTurnPlans(Strategy strategy) {
		List<TurnPlan> turnPlans = new();

		for (int i = 0; i < strategy.gambits.Count; ++i) {
			Gambit gambit = strategy.gambits[i];
			for (int ii = 0; ii < strategy.objectiveTypes.Count; ++ii) {
				ObjectiveType objectiveType = strategy.objectiveTypes[ii];
				if (!gambit.IsViable(BC)) {
					continue;
				}
				Debug.Log("Evaluating gambit " + gambit.name + " with " + objectiveType.ToString());
				Strategem strategem = new(gambit, objectiveType, strategy.gambits.Count - i + (strategy.objectiveTypes.Count - ii) );

				// Determine where to move and aim to best use the ability
				AbilityRange range = gambit.ability.GetComponent<AbilityRange>();
				if (range.positionOriented == false)
					// It doesn't matter where you stand
					turnPlans.AddRange(
						PlanPositionIndependent(strategem)
					);
				else if (!range.directionOriented)
					// It DOES matter where you stand, but it doesn't matter where you face
					turnPlans.AddRange(
						PlanDirectionIndependent(strategem)
					);
				else
					// It DOES matter where you stand and it DOES matter where you face
					turnPlans.AddRange(
						PlanDirectionDependent(strategem)
					);
			}
		}
		return turnPlans;
	}
}