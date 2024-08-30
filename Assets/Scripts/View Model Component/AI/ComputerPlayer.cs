using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

public class ComputerPlayer : MonoBehaviour {
	#region Fields
	public BattleController BC;
	public Unit actor { get { return BC.turn.actor; }}
	AwarenessController AC { get { return BC.awarenessController; } }
	PatrolController PC { get { return BC.patrolController; } }
	Alliance actorAlliance { get { return actor.GetComponent<Alliance>(); }}
	Gambit strategy { get { return actor.GetComponent<Gambit>(); }}
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

		// Are the conditions met for the highest priority gambit?
		// Can the ability be used?
		Strategy strategy = actor.GetComponentInChildren<Strategy>();
		TurnPlan plan = new TurnPlanFactory(this).EvaluateStrategy(strategy);

        // If none of the gambit conditions could be met,
        // investigate or patrol.
        plan ??= InvestigateOrPatrol();

        // If this unit is preoccupied, make sure they're not on patrol anymore
        if (topPriorityFoeAwareness != null || topPriorityInterestAwareness != null) {
			PC.RemoveUnitFromPatrol(actor);
		}

		// Return the completed plan
		return plan;
	}

	public List<Tile> GetMoveOptions() {
		List<Tile> tiles = actor.GetComponent<Movement>().GetTilesInRange(BC.board);
		// Add the tile the actor is on now as a viable move option.
		tiles.Add(actor.tile);
		List<Tile> unoccupiedTilesInRange = tiles.Where((t) => t.occupant != actor).ToList();
		List<Tile> unrestrictedTiles = tiles.Where((t) => !BC.board.exitMarkers.Select(e => e.position).Contains(t.pos)).ToList();
		List<Tile> orderedTiles = unrestrictedTiles.OrderBy(tile => tile.pos.x).ThenBy(tile => tile.pos.y).ToList();
		return orderedTiles;
	}

	public int GetMoveScoreForObjective(Tile potentialMoveTile, ObjectiveType objectiveType) {
		switch (objectiveType) {
			case ObjectiveType.BlockExit:
				// Find tiles between the exit and all known foes
				// then look for overlap with the existing tiles
				/// Find nearest exit
				Tile nearestExitMarkerTile = null;
				int nearestExitMarkerTilePathLength = int.MaxValue;
				for (int i = 0; i < BC.board.exitMarkers.Count; i++) {
					ExitMarker exitMarker = BC.board.exitMarkers[i];
					Tile exitMarkerTile = BC.board.GetTile(exitMarker.position);
					BC.board.FindPath(actor, actor.tile, exitMarkerTile, delegate (List<Tile> finalPath) {
						if (finalPath.Count < nearestExitMarkerTilePathLength) {
							nearestExitMarkerTile = exitMarkerTile;
						}
					});
				}
				/// Get paths to exit for each foe's LAST known location
				List<Awareness> awarenesses = AC.TopAwarenesses(actor);
				HashSet<Tile> pathsToExit = new HashSet<Tile>();
				for (int i = 0; i < awarenesses.Count; i++) {
					Awareness awareness = awarenesses[i];
					BC.board.FindPath(awareness.stealth.unit,
									  BC.board.GetTile(awareness.pointOfInterest),
									  nearestExitMarkerTile,
									  delegate (List<Tile> finalPath) {
						finalPath.ForEach(tile => pathsToExit.Add(tile));
					});
				} 

				/// Remove exit tile itself
				pathsToExit.Remove(nearestExitMarkerTile);

				int score = 0;
				/// Add point if tile is along a foe's path
				if (pathsToExit.Contains(potentialMoveTile)) {
					score++;
				}
				/// Rate each tile based on its distance from the exit!!!!
				int movementRange = actor.GetComponent<Stats>()[StatTypes.MOV];
				Point distance = nearestExitMarkerTile.pos - potentialMoveTile.pos;
				int distanceRaw = Mathf.Abs(distance.x) + Mathf.Abs(distance.y);
				score += movementRange - distanceRaw;
				return score;
			case ObjectiveType.ProtectSelf:
				// Among the existing tiles, find the ones furthest from all known foes
				return 0;
			default:
				return 0;
		}
	}
	#endregion
	
	#region Private

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
			Tile destination = finalPath.Count > 0 ? finalPath.Last() : null;
			if (destination != null)
				plan.moveLocation = FindNearestMoveOptionToTile(destination);
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
				var directions2 = origin.GetDirections(topPriorityInterestAwareness.stealth.unit.tile);
				if (directions2.Count > 0) {
					dir = directions2.First();
					Debug.Log("Facing top priority foe (best guess): " + dir);
				} else {
					dir = actor.dir; // Just continue facing in the current direction.
					Debug.Log("Continuing to face the current direction.");
				}
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

	public bool ShouldActorPrepareForNextTurn() {
		// If the actor has a target and can move, they should do so
		return topPriorityFoeAwareness != null && canActorPerformMoveAction;
	}

	public bool IsMissileImpeded(Unit caster, Ability ability, Tile destination) {
		ConstantAbilityRange range = ability.GetComponent<ConstantAbilityRange>();
		if (range == null) return false;
		if (!range.isMissile) return false;
		if (BC.board.WallImpedingMissile(caster.tile, destination.pos) != null ||
			BC.board.UnitImpedingMissile(caster.tile, destination.pos) != null) {
			return true;
		}
		return false;
	}

	Tile FindNearestMoveOptionToTile(Tile tile) {
		var moveOptions = GetMoveOptions();
		Tile destination = null;
		if (moveOptions.Contains(tile)) {
			return tile;
		} else {
			BC.board.FindPath(BC.turn.actor, BC.turn.actor.tile, BC.board.GetTile(tile.pos), delegate (List<Tile> finalPath) {
				Tile toCheck = tile;
				while (toCheck != null) {
					if (moveOptions.Contains(toCheck)) {
						// Move toward top awareness / point of interest
						destination = toCheck;
						break;
					}
					// Board search keeps previous tiles in memory
					toCheck = toCheck.prev;
				}
			});
		}
		return destination;
	}

	TurnPlan PrepareForNextTurn(TurnPlan plan) {
		Debug.Log("Preparing for next turn.");
		if (!canActorPerformMajorAction ) {

		}
		if (!canActorPerformMoveAction) {

		}
		return plan;
	}

	#endregion
}