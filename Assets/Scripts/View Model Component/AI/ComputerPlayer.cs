using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

public class ComputerPlayer : MonoBehaviour {
	#region Fields
	BattleController BC;
	Unit actor { get { return BC.turn.actor; }}
	AwarenessController AC { get { return BC.awarenessController; } }
	PatrolController PC { get { return BC.patrolController; } }
	Alliance actorAlliance { get { return actor.GetComponent<Alliance>(); }}
	bool canActorPerformMoveAction { get { return BC.turnOrderController.CanActorPerformActionType(ActionType.Move); }}
	bool canActorPerformMajorAction { get { return BC.turnOrderController.CanActorPerformActionType(ActionType.Major); }}
	Awareness topPriorityFoeAwareness;
	Awareness topPriorityInterestAwareness;
	#endregion
	
	#region MonoBehaviour
	void Awake() {
		BC = GetComponent<BattleController>();
		InputController.submitEvent += OnSubmit;
	}

	bool isPaused = false;
	void OnSubmit() {
		isPaused = false;
	}
	#endregion

	#region Public
	// Create and fill out a turn plan
	public TurnPlan FormulatePlan() {
		Debug.Log(actor.name + " is formulating a plan.");
		SetTopPriorityFoeAndPointOfInterest();

		TurnPlan plan = new TurnPlan();

		// Are the conditions met for the highest priority gambit?
		// Can the ability be used?
		GambitSet gambitSet = actor.GetComponentInChildren<GambitSet>();
		Gambit gambit = gambitSet.PickGambit(BC, (Gambit g) => {
			Debug.Log("Evaluating " + g.name);
			plan = EvaluateGambit(g);
			return plan != null;
		});

		// If none of the gambit conditions could be met,
		// investigate or patrol.
		if (gambit == null) {
			plan = InvestigateOrPatrol();
		}

		// If this unit is preoccupied, make sure they're not on patrol anymore
		if (topPriorityFoeAwareness != null || topPriorityInterestAwareness != null) {
			PC.RemoveUnitFromPatrol(actor);
		}

		// Return the completed plan
		return plan;
	}
	#endregion
	
	#region Private

	TurnPlan EvaluateGambit(Gambit gambit) {
		// Determine where to move and aim to best use the ability
		AbilityRange range = gambit.ability.GetComponent<AbilityRange>();
		if (range.positionOriented == false)
			// It doesn't matter where you stand
			return PlanPositionIndependent(gambit);
		else if (!range.directionOriented)
			// It DOES matter where you stand, but it doesn't matter where you face
			return PlanDirectionIndependent(gambit);
		else
			// It DOES matter where you stand and it DOES matter where you face
			return PlanDirectionDependent(gambit);
	}

	/* When it is determined that an ability is position independent,
	 * then I simply move to a random tile within the unit’s move range, because position didn’t matter.
	 * There is room to polish this up –
	 * perhaps the unit would rather move toward or away from its foes during this time.
	 * If you want more specific behavior like that it shouldn’t be hard to add.
	 * I’ll show an example of moving toward the nearest foe soon. */
	TurnPlan PlanPositionIndependent(Gambit gambit) {
		List<Tile> moveOptions = GetMoveOptions();
		// TODO: Have the unit move somewhere logical, instead of moving randomly
		Tile tile = moveOptions[Random.Range(0, moveOptions.Count - 1)];
		Debug.Log("moveOptions.Count is " + moveOptions.Count + ". Randomly moving to " + tile);
		TurnPlan plan = new(gambit);
		plan.moveLocation = plan.fireLocation = tile;
		return plan;
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
	TurnPlan PlanDirectionIndependent(Gambit gambit) {
		Tile startTile = actor.tile;
		Dictionary<Tile, PlanScratchPad> planScratchPadByTile = new Dictionary<Tile, PlanScratchPad>();
		AbilityRange ar = gambit.ability.GetComponent<AbilityRange>();
		List<Tile> moveOptions = GetMoveOptions();
		
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
                    planScratchPad = new PlanScratchPad();
					// Only add an ability target if the unit can perform a major action.
					if (canActorPerformMajorAction) {
						planScratchPad.abilityTargetTile = abilityTargetOption;
						planScratchPad.direction = actor.dir;
						planScratchPad = RateFireLocation(gambit, planScratchPad);
					}
					planScratchPadByTile[abilityTargetOption] = planScratchPad;
				}
				// Only add a move target if the unit can perform a move action.
				if (canActorPerformMoveAction) {
					planScratchPad.AddMoveTarget(moveTile);
				}
			}
		}
		
		actor.Place(startTile);
		List<PlanScratchPad> planScratchPads = new List<PlanScratchPad>(planScratchPadByTile.Values);
		Debug.Log("planScratchPads.Count: " + planScratchPads.Count);
		PlanScratchPad bestPlanScratchPad = PickBestPlanScratchPad(gambit.ability, planScratchPads);

		if (bestPlanScratchPad == null) return null;

		TurnPlan plan = new(gambit) {
			fireLocation = bestPlanScratchPad.abilityTargetTile,
			attackDirection = bestPlanScratchPad.direction,
			moveLocation = bestPlanScratchPad.bestMoveTile
		};

		return plan;
	}

	/* This last case depends both on a unit’s position on the board
	 * and the direction the unit faces while using the selected ability.
	 * It should look pretty similar to the “PlanDirectionIndependent” variation.
	 * The main difference here is that instead of grabbing the Ability Range component
	 * and looping through targeted tiles, we instead loop through each of the four facing directions.
	 * Every single entry generated will have a unique area of effect –
	 * there is no overlap or need for the dictionary as I had last time.
	 * We can simply track each entry in a list directly. */
	TurnPlan PlanDirectionDependent(Gambit gambit) {
		Tile startTile = actor.tile;
		Direction startDirection = actor.dir;
		List<PlanScratchPad> planScratchPads = new List<PlanScratchPad>();
		List<Tile> moveOptions = GetMoveOptions();
		
		for (int i = 0; i < moveOptions.Count; ++i) {
			Tile moveTile = moveOptions[i];
			actor.Place( moveTile );
			
			for (int ii = 0; ii < 4; ++ii) {
				actor.dir = (Direction)ii;
                PlanScratchPad planScratchPad = new PlanScratchPad {
                    abilityTargetTile = moveTile,
                    direction = actor.dir
                };
                planScratchPad = RateFireLocation(gambit, planScratchPad);
				planScratchPad.AddMoveTarget(moveTile);
				planScratchPads.Add(planScratchPad);
			}
		}
		
		actor.Place(startTile);
		actor.dir = startDirection;
		PlanScratchPad bestOption = PickBestPlanScratchPad(gambit.ability, planScratchPads);
		
		if (bestOption == null) return null;

		TurnPlan plan = new(gambit) {
			fireLocation = bestOption.abilityTargetTile,
			attackDirection = bestOption.direction,
			moveLocation = bestOption.bestMoveTile
		};

		return plan;
	}
	
	public List<Tile> GetMoveOptions() {
		List<Tile> tiles = actor.GetComponent<Movement>().GetTilesInRange(BC.board);
		// Add the tile the actor is on now as a viable move option.
		tiles.Add(actor.tile);
		List<Tile> unoccupiedTiles = tiles.Where((t) => t.occupant != actor).ToList();
		return unoccupiedTiles.OrderBy(tile => tile.pos.x).ThenBy(tile => tile.pos.y).ToList();
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
	PlanScratchPad PickBestPlanScratchPad(Ability ability, List<PlanScratchPad> planScratchPads) {

		int bestScore = 1;
		List<PlanScratchPad> bestPlanScratchPads = new List<PlanScratchPad>();
		for (int i = 0; i < planScratchPads.Count; ++i) {
			PlanScratchPad planScratchPad = planScratchPads[i];
			int score = planScratchPad.GetScore(actor, ability);
			
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

	// Investigation Methods

	TurnPlan InvestigateOrPatrol() {
		TurnPlan plan = null;
		if (topPriorityInterestAwareness != null) {
			// Just position yourself better for the next turn.
			/// TODO: Update the PlanScratchPad algorithm to just give us the best move option for next turn.
			plan = Investigate(topPriorityFoeAwareness ?? topPriorityInterestAwareness);
		} else {
			Debug.Log("Going on patrol");
			Patrol patrol = PC.GetPatrolForUnit(actor);
			if (patrol != null) {
				plan = patrol.GetPlan(actor, BC.board);
			} else {
				List<Tile> moveOptions = GetMoveOptions();
				PC.GetNearestAvailablePatrol(actor, delegate (Patrol p) {
					if (p != null) {
						plan = p.GetPlan(actor, BC.board);
					} else {
						Console.Main.Log("No patrol found.");
					}
				});
			}
		}
		return plan;
	}

	TurnPlan Investigate(Awareness awareness) {
		if (awareness == topPriorityFoeAwareness) {
			Debug.Log("Investigating topPriorityFoeAwareness");
		}
		if (awareness == topPriorityInterestAwareness) {
			Debug.Log("Investigating topPriorityInterestAwareness");
		}
		TurnPlan plan = new();
		if (awareness == null) return plan;
		Tile topPriorityTileOfInterest = BC.board.GetTile(awareness.pointOfInterest);
		List<Tile> moveOptions = GetMoveOptions();
		BC.board.FindPath(actor, actor.tile, topPriorityTileOfInterest, delegate (List<Tile> finalPath) {
			Console.Main.Log(string.Format("{0} is investigating {1}", actor.name, topPriorityTileOfInterest.ToString()));
			plan.moveLocation = finalPath.Count > 0 ? finalPath.Last() : null;
		});
		return plan;
	}

	void SetTopPriorityFoeAndPointOfInterest() {
		topPriorityFoeAwareness = null;
		topPriorityInterestAwareness = null;

		List<Awareness> topAwarenesses = AC.TopAwarenesses(actor).FindAll( delegate (Awareness a) {
			Alliance otherAlliance = a.stealth.unit.GetComponentInChildren<Alliance>();
			var knockout = a.stealth.unit.KO;
			return actorAlliance.IsMatch(otherAlliance, TargetType.Foe) && knockout == null;
		});
		if (topAwarenesses.Count == 0) return;

		foreach (Awareness a in topAwarenesses) {
			if (a.type == AwarenessType.Seen) {
				// Find the nearest potential foe
				if (topPriorityFoeAwareness != null) {
					// This new foe is the top priority if they're closer.
					Unit topPriorityFoe = topPriorityFoeAwareness.stealth.unit;
					int distanceToCurrentFoe = BC.board.GetDistance(actor.tile, a.stealth.unit.tile);
					int distanceToPotentialFoe = BC.board.GetDistance(actor.tile, a.stealth.unit.tile);
					topPriorityFoeAwareness = distanceToPotentialFoe < distanceToCurrentFoe ? a : topPriorityFoeAwareness;
					topPriorityInterestAwareness = topPriorityFoeAwareness; // Also set top priotiy tile of interest
				} else {
					// This foe is the top priority.
					topPriorityFoeAwareness = a;
					topPriorityInterestAwareness = topPriorityFoeAwareness; // Also set top priotiy tile of interest
                }
            } else {
				// Find the nearest potential point of interest
				if (topPriorityInterestAwareness != null) {
					// This new point of interest is the top priority if they're closer.
					int distanceToCurrentPointOfInterest = BC.board.GetDistance(actor.tile, BC.board.GetTile(a.pointOfInterest));
					int distanceToPotentialPointOfInterest = BC.board.GetDistance(actor.tile, BC.board.GetTile(a.pointOfInterest));
					// If the potential point of interest is where the unit is already,
					// it's not worth investigating.
					if (distanceToPotentialPointOfInterest != 0)
						topPriorityFoeAwareness = distanceToPotentialPointOfInterest < distanceToCurrentPointOfInterest ? a : topPriorityFoeAwareness;
				} else {
					// This point of interest is the top priority.
					topPriorityInterestAwareness = a;
				}
			}
        }
	}

	/* After we have moved and used an ability, we need to determine an end facing direction.
	 * For this I find the nearest foe again. Note that it is important to do this a second time,
	 * because the foe who was nearest before you moved is not necessarily the foe who is nearest after you have moved.
	 * Next I loop through each of the directions until I find a direction which has me face the foe from the front.
	 * This way the foe is less likely to be able to attack me from the back. */
	public Direction DetermineEndFacingDirection() {
		SetTopPriorityFoeAndPointOfInterest();
		Direction dir = (Direction)UnityEngine.Random.Range(0, 4);
		//TODO: Avoid showing back to foe, but try to face a point of interest
		if (topPriorityFoeAwareness != null) {
			// Try to face the foe and turn your back away from them
			Direction start = actor.dir;
			for (int i = 0; i < 4; ++i) {
				actor.dir = (Direction)i;
				if (topPriorityFoeAwareness.stealth.unit.GetFacing(actor) == Facings.Front) {
					dir = actor.dir;
					Debug.Log("Facing top priority foe: " + dir);
					break;
				}
			}
			actor.dir = start;
		} else if (topPriorityInterestAwareness != null) {
			// Try to face the tile of interest
			// *** TODO: Find some other way to access the TurnPlan than this. Maybe make it a class property?
			var origin = BC.turn.plan.moveLocation != null ? BC.turn.plan.moveLocation : actor.tile;
			var interestingTile = BC.board.GetTile(topPriorityInterestAwareness.pointOfInterest);
			var directions = origin.GetDirections(interestingTile);
			if (directions.Count > 0) {
				dir = directions.First();
				Debug.Log("Facing top priority interest (best guess): " + dir);
			} else {
				dir = actor.dir; // Just continue facing in the current direction.
				Debug.Log("Continuing to face the current direction.");
			}
		} else {
			Patrol patrol = PC.GetPatrolForUnit(actor);
			if (patrol != null) {
				// If patrolling, end in the patrol 
				dir = patrol.GetCurrentDirection();
				Debug.Log("Facing in patrol direction: " + dir);
			}
		}
		return dir;
	}

	public void HandleEndOfInvestigation() {
		if (topPriorityInterestAwareness != null) {
			var interestingTile = BC.board.GetTile(topPriorityInterestAwareness.pointOfInterest);
			bool didUnitFinishInvestigation = topPriorityInterestAwareness != null && actor.tile == interestingTile;
			// Investigation is done, but target of interest was not found
			if (didUnitFinishInvestigation && topPriorityFoeAwareness != null && topPriorityFoeAwareness.type != AwarenessType.Seen) {
				BC.awarenessController.UpdateAwareness(topPriorityInterestAwareness, AwarenessType.Unaware, topPriorityInterestAwareness.pointOfInterest);
				SetTopPriorityFoeAndPointOfInterest();
				if (topPriorityInterestAwareness != null) {
					HandleEndOfInvestigation();
				}
			}
		}
	}

	public bool CanActorContinue() {
		// If the actor has a target and can act to reach it, do so
		return topPriorityFoeAwareness != null && (canActorPerformMajorAction || canActorPerformMoveAction);
	}

	#endregion
}