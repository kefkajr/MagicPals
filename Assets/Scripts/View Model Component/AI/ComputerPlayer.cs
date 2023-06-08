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
	Perception perception { get { return actor.GetComponent<Perception>(); } }
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
	// Create and fill out a plan of attack
	public IEnumerator Evaluate(Turn turn) {
		SetTopPriorityFoeAndPointOfInterest();

		PlanOfAttack poa = new PlanOfAttack();

		if (topPriorityFoeAwareness != null) {
			// Step 1: Decide what ability to use
			AttackPattern pattern = actor.GetComponentInChildren<AttackPattern>();
			if (pattern)
				pattern.Pick(BC, poa);
			else
				yield return DefaultAttackPattern(poa);

			Console.Main.Log(string.Format("{0} wants to use {1}", actor.name, poa.ability.name));

			// Step 2: Determine where to move and aim to best use the ability
			if (IsPositionIndependent(poa))
				// It doesn't matter where you stand
				yield return PlanPositionIndependent(poa);
			else if (IsDirectionIndependent(poa))
				// It DOES matter where you stand, but it doesn't matter where you face
				yield return PlanDirectionIndependent(poa);
			else
				// It DOES matter where you stand and it DOES matter where you face
				yield return PlanDirectionDependent(poa);
		}
		if (poa.ability == null) {
			if (topPriorityInterestAwareness != null) {
				// Just position yourself better for the next turn.
				/// TODO: Update the attack option algorithm to just give us the best move option for next turn.
				yield return Investigate(poa);
			} else {
				Patrol patrol = PC.GetPatrolForUnit(actor);
				if (patrol != null) {
					patrol.SetPlan(poa, actor, BC.board);
				} else {
					List<Tile> moveOptions = GetMoveOptions();
					yield return PC.GetNearestAvailablePatrol(actor, delegate (Patrol p) {
						if (p != null) {
							p.SetPlan(poa, actor, BC.board);
						} else {
							Console.Main.Log("No patrol found.");
						}
					});
				}
			}
		}

		if (topPriorityFoeAwareness != null || topPriorityInterestAwareness != null) {
			// If this unit is preoccupied, make sure they're not on patrol anymore
			PC.RemoveUnitFromPatrol(actor);
		}

		// Find the nearest viable move option, if the current planned location is too far
		if (poa.moveLocation != null) {
			List<Tile> moveOptions = GetMoveOptions();
			if (!moveOptions.Contains(poa.moveLocation)) {
				yield return BC.board.FindPath(actor, actor.tile, BC.board.GetTile(poa.moveLocation.pos), delegate (List<Tile> finalPath) {
					poa.moveLocation = FindNearestMoveOptionToTile(moveOptions, poa.moveLocation);
				});
			}
		}	

		// Step 3: Return the completed plan
		turn.plan = poa;
	}
	#endregion
	
	#region Private
	IEnumerator DefaultAttackPattern(PlanOfAttack poa) {
		// Just get the first "Attack" ability
		poa.ability = actor.GetComponentInChildren<Ability>();
		poa.targetType = TargetType.Foe;
		yield return null;
	}

	bool IsPositionIndependent(PlanOfAttack poa) {
		AbilityRange range = poa.ability.GetComponent<AbilityRange>();
		return range.positionOriented == false;
	}
	
	bool IsDirectionIndependent(PlanOfAttack poa) {
		AbilityRange range = poa.ability.GetComponent<AbilityRange>();
		return !range.directionOriented;
	}

	/* When it is determined that an ability is position independent,
	 * then I simply move to a random tile within the unit’s move range, because position didn’t matter.
	 * There is room to polish this up –
	 * perhaps the unit would rather move toward or away from its foes during this time.
	 * If you want more specific behavior like that it shouldn’t be hard to add.
	 * I’ll show an example of moving toward the nearest foe soon. */
	IEnumerator PlanPositionIndependent(PlanOfAttack poa) {
		List<Tile> moveOptions = GetMoveOptions();
		// TODO: Have the unit move somewhere logical, instead of moving randomly
		Tile tile = moveOptions[Random.Range(0, moveOptions.Count - 1)];
		poa.moveLocation = poa.fireLocation = tile;
		yield return null;
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
	 * Finally, I pass the list of options we have built up to this point to a method
	 * which can pick the best overall option for our turn. */
	IEnumerator PlanDirectionIndependent(PlanOfAttack poa) {
		Tile startTile = actor.tile;
		Dictionary<Tile, AttackOption> attackOptionsByTile = new Dictionary<Tile, AttackOption>();
		AbilityRange ar = poa.ability.GetComponent<AbilityRange>();
		List<Tile> moveOptions = GetMoveOptions();
		
		for (int i = 0; i < moveOptions.Count; ++i) {
			Tile moveTile = moveOptions[i];
			actor.Place( moveTile );
			List<Tile> fireOptions = ar.GetTilesInRange(BC.board).OrderBy(tile => tile.pos.x).ThenBy(tile => tile.pos.y).ToList();;
			
			for (int j = 0; j < fireOptions.Count; ++j) {
				Tile fireTile = fireOptions[j];
				AttackOption attackOption = null;
				if (attackOptionsByTile.ContainsKey(fireTile)) {
					attackOption = attackOptionsByTile[fireTile];
				} else {
					attackOption = new AttackOption();
					attackOptionsByTile[fireTile] = attackOption;
					attackOption.target = fireTile;
					attackOption.direction = actor.dir;
					yield return RateFireLocation(poa, attackOption);
				}

				attackOption.AddMoveTarget(moveTile);

				// isPaused = GameConfig.Main.DebugComputerPlayer;
				// int score = attackOption.GetScore(actor, poa.ability);
				// Console.Main.Log(string.Format("Option {0} - Score: {1}",
				// 	j, score));
				
				// BC.board.SelectTiles(new List<Tile>{moveTile}, Color.green);
				// BC.board.SelectTiles(attackOption.areaTargets, Color.cyan);
				// BC.board.SelectTiles(attackOption.marks.Select(mark => mark.tile).ToList(), Color.magenta);
				// BC.board.SelectTiles(attackOption.marks.Where(mark => mark.isMatch).Select(mark => mark.tile).ToList(), Color.red);
				// BC.board.SelectTiles(new List<Tile>{attackOption.target}, Color.blue);

				// while(isPaused) {
				// 	yield return null;
				// }
				// BC.board.DeSelectAllTiles();
				
			}
		}
		
		actor.Place(startTile);
		List<AttackOption> attackOptions = new List<AttackOption>(attackOptionsByTile.Values);
		yield return PickBestOption(poa, attackOptions);
	}

	/* This last case depends both on a unit’s position on the board
	 * and the direction the unit faces while using the selected ability.
	 * It should look pretty similar to the “PlanDirectionIndependent” variation.
	 * The main difference here is that instead of grabbing the Ability Range component
	 * and looping through targeted tiles, we instead loop through each of the four facing directions.
	 * Every single entry generated will have a unique area of effect –
	 * there is no overlap or need for the dictionary as I had last time.
	 * We can simply track each entry in a list directly. */
	IEnumerator PlanDirectionDependent(PlanOfAttack poa) {
		Tile startTile = actor.tile;
		Direction startDirection = actor.dir;
		List<AttackOption> attackOptions = new List<AttackOption>();
		List<Tile> moveOptions = GetMoveOptions();
		
		for (int i = 0; i < moveOptions.Count; ++i) {
			Tile moveTile = moveOptions[i];
			actor.Place( moveTile );
			
			for (int ii = 0; ii < 4; ++ii) {
				actor.dir = (Direction)ii;
				AttackOption attackOption = new AttackOption();
				attackOption.target = moveTile;
				attackOption.direction = actor.dir;
				yield return RateFireLocation(poa, attackOption);
				attackOption.AddMoveTarget(moveTile);
				attackOptions.Add(attackOption);
			}
		}
		
		actor.Place(startTile);
		actor.dir = startDirection;
		yield return PickBestOption(poa, attackOptions);
	}
	
	List<Tile> GetMoveOptions() {
		List<Tile> tiles = actor.GetComponent<Movement>().GetTilesInRange(BC.board);
		List<Tile> unoccupiedTiles = tiles.Where((t) => t.occupant == null).ToList();
		return unoccupiedTiles.OrderBy(tile => tile.pos.x).ThenBy(tile => tile.pos.y).ToList();
	}

	/* As we were creating each Attack Option (a note on the effect area of using an ability),
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
	IEnumerator RateFireLocation (PlanOfAttack poa, AttackOption option) {
		AbilityArea area = poa.ability.GetComponent<AbilityArea>();
		List<Tile> tiles = area.GetTilesInArea(BC.board, option.target.pos);
		option.areaTargets = tiles;
		option.isCasterMatch = IsAbilityTargetMatch(poa, actor.tile);

		for (int i = 0; i < tiles.Count; ++i) {
			Tile tile = tiles[i];
			if (actor.tile == tiles[i] || !poa.ability.IsTarget(tile))
				continue;
			
			bool isMatch = IsAbilityTargetMatch(poa, tile);
			option.AddMark(tile, isMatch);
		}
		yield return null;
	}

	/* This method shows how to determine which marks are a match or not.
	 * An ability which targets a tile is simply marked as true
	 * (I havent actually implemented any such abilities, so I might change this logic later).
	 * Otherwise, I use the alliance component to determine whether or not the target type is a match. */
	bool IsAbilityTargetMatch(PlanOfAttack poa, Tile tile) {
		bool isMatch = false;
		if (poa.targetType == TargetType.Tile)
			isMatch = true;
		else if (poa.targetType != TargetType.None) {
			// TODO: Revisit this after implemeting GAMBITS or some other AI method

			Alliance targetAlliance = tile.occupant.GetComponentInChildren<Alliance>();
			Unit targetUnit = targetAlliance.GetComponent<Unit>();
			if (targetAlliance != null) {
				if (actorAlliance.IsMatch(targetAlliance, poa.targetType)) {
					if (actorAlliance == targetAlliance &&
					  (poa.targetType == TargetType.Self || poa.targetType == TargetType.AllyOrSelf)) {
						isMatch = true;
					} else if (poa.targetType == TargetType.Ally) {
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

	/* This is the method that actually provides a score for each of the attack options.
	 * It goes through two “passes” of analyzing our options.
	 * On the first pass, it scores each attack option based on having
	 * more marks which are matches than marks which are not matches.
	 * 
	 * Whenever I find a new “best” score, I track what the score was,
	 * and add the ability to a list of the options which I consider to be the best.
	 * This list will be cleared if I should find a better score,
	 * but if I find additional options with a tied score then I will also add them to the list.
	 * 
	 * When all of the options have been scored, it is actually possible
	 * that I wont have any entries in my best options list.
	 * This would be the case where an ability could technically be used,
	 * but the effect would be detrimental to the user’s party.
	 * For example, if the only option an AI unit had to attack was one of its allies,
	 * then it would be better not to do anything than to actually perform the ability.
	 * In these cases, I mark the plan’s abilty as null so that it wont be performed.
	 * 
	 * In the cases where I do have some beneficial options to pick from,
	 * I will then run another pass to help trim down the options even further.
	 * There are multiple reasons for this. For example,
	 * lets say I can attack a target unit from multiple different move locations.
	 * Some of those locations may be from the front, while others may be from the back.
	 * If I can pick, I would want to pick an angle from the back
	 * so that my chances of the attack hitting are greater.
	 * 
	 * By the end of this second “pass” I should have one or more options which were added to the final picks.
	 * Because they all share the same score,
	 * I pick any of them at random and assign the relevant details to our plan of attack. */
	IEnumerator PickBestOption(PlanOfAttack poa, List<AttackOption> options) {

		int bestScore = 1;
		List<AttackOption> bestOptions = new List<AttackOption>();
		for (int i = 0; i < options.Count; ++i) {
			isPaused = GameConfig.Main.DebugComputerPlayer;

			AttackOption option = options[i];
			int score = option.GetScore(actor, poa.ability);
			// Console.Main.Log(string.Format("Option {0} - Best Move: {1}, Target: {2}, Score: {3}", i, option.bestMoveTile.ToString(), option.target.ToString(), score));

			BC.board.HighlightTiles(new List<Tile>{option.bestMoveTile}, TileHighlightColorType.moveRangeHighlight);
			BC.board.HighlightTiles(option.areaTargets, TileHighlightColorType.targetRangeHighlight);
			BC.board.HighlightTiles(option.marks.Select(mark => mark.tile).ToList(), TileHighlightColorType.targetAreaHighlight);
			BC.board.HighlightTiles(option.marks.Where(mark => mark.isMatch).Select(mark => mark.tile).ToList(), TileHighlightColorType.viewingRangeHighlight);
			BC.board.HighlightTiles(new List<Tile>{option.target}, TileHighlightColorType.viewingRangeEdgeHighlight);

			while(isPaused) {
				yield return null;
			}

			BC.board.DeHighlightAllTiles();
			
			if (score > bestScore) {
				bestScore = score;
				bestOptions.Clear();
				bestOptions.Add(option);
			} else if (score == bestScore) {
				bestOptions.Add(option);
			}
		}

		if (bestOptions.Count == 0) {
			poa.ability = null; // Clear ability as a sign not to perform it
			yield break;
		}

		List<AttackOption> finalPicks = new List<AttackOption>();
		bestScore = 0;
		for (int i = 0; i < bestOptions.Count; ++i) {
			AttackOption option = bestOptions[i];
			int score = option.bestAngleBasedScore;
			if (score > bestScore) {
				bestScore = score;
				finalPicks.Clear();
				finalPicks.Add(option);
			} else if (score == bestScore) {
				finalPicks.Add(option);
			}
		}

		AttackOption choice = finalPicks[ UnityEngine.Random.Range(0, finalPicks.Count)];
		poa.fireLocation = choice.target;
		poa.attackDirection = choice.direction;
		poa.moveLocation = choice.bestMoveTile;
		yield return null;
	}

	// NEW Investigation Methods

	IEnumerator Investigate(PlanOfAttack poa) {
		if (topPriorityInterestAwareness != null) {
			Tile topPriorityTileOfInterest = BC.board.GetTile(topPriorityInterestAwareness.pointOfInterest);
			List<Tile> moveOptions = GetMoveOptions();
			yield return BC.board.FindPath(actor, actor.tile, topPriorityTileOfInterest, delegate (List<Tile> finalPath) {
				Console.Main.Log(string.Format("{0} is investigating {1}", actor.name, topPriorityTileOfInterest.ToString()));
				poa.moveLocation = finalPath.Count > 0 ? finalPath.Last() : null;
			});
		}
		yield return null;
	}

	Tile FindNearestMoveOptionToTile(List<Tile> moveOptions, Tile tile) {
		Tile toCheck = tile;
		while (toCheck != null) {
			if (moveOptions.Contains(toCheck)) {
				// Move toward top awareness / point of interest
				return toCheck;
			}
			// Board search keeps previous tiles in memory
			toCheck = toCheck.prev;
		}
		return toCheck;
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
					break;
				}
			}
			actor.dir = start;
		} else if (topPriorityInterestAwareness != null) {
			// Try to face the tile of interest
			// *** TODO: Find some other way to access the PlanOfAttack than this. Maybe make it a class property?
			var origin = BC.turn.plan.moveLocation != null ? BC.turn.plan.moveLocation : actor.tile;
			var interestingTile = BC.board.GetTile(topPriorityInterestAwareness.pointOfInterest);
			var directions = origin.GetDirections(interestingTile);
			if (directions.Count > 0)
				dir = directions.First();
			else
				dir = actor.dir; // Just continue facing in the current direction.
		} else {
			Patrol patrol = PC.GetPatrolForUnit(actor);
			if (patrol != null) {
				// If patrolling, end in the patrol 
				dir = patrol.GetCurrentDirection();
			}
		}
		return dir;
	}

	public void HandleEndOfInvestigation() {
		if (topPriorityInterestAwareness != null) {
			var interestingTile = BC.board.GetTile(topPriorityInterestAwareness.pointOfInterest);
			bool didUnitFinishInvestigation = topPriorityInterestAwareness != null && actor.tile == interestingTile;
			if (didUnitFinishInvestigation) {
				BC.awarenessController.UpdateAwareness(topPriorityInterestAwareness, AwarenessType.Unaware, topPriorityInterestAwareness.pointOfInterest);
				SetTopPriorityFoeAndPointOfInterest();
				if (topPriorityInterestAwareness != null) {
					HandleEndOfInvestigation();
				}
			}
		}
	}

	#endregion
}