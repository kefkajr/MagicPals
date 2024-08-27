using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms.Impl;

/* As soon as it is determined that it is the computer’s turn to make a move,
 * we will need to formulate a turn plan. This means I decide what ability to use,
 * who or what to use the ability on, where I move to on the board,
 * and where I cast the ability or which direction I face while casting the ability.
 * All of this data is stored in a simple object which will be populated by various steps of the AI process. */

public class TurnPlan {
	public Strategem strategem;
	public Ability ability;
	public TargetType targetType;
	public Tile moveLocation;
	public Tile fireLocation;
	public Direction attackDirection;
	int _score = 0;
	public int score { get { return _score;}}
	string _description = "";
	public string description { get { return _description;}}
	public bool IsEmpty { get { return ability == null && moveLocation == null; }}

	public TurnPlan() {}
	public TurnPlan(Strategem strategem) {
		this.strategem = strategem;
		this.ability = strategem.gambit.ability;
		this.targetType = strategem.gambit.targetType;
	}

	public void CalculateScore(BattleController BC, Unit actor) {
		_score = 0;
		_description = "";
		CalculateScoreForPosition(BC, actor);
		CalculateMoveScoreForObjective(BC, actor);
		RateFireLocation(BC, actor);
		_score += strategem.priorityBonus;
		_description += "Adding priority bonus " + strategem.priorityBonus.ToString() + ". / Score is " + score.ToString() + ".";
	}

	void CalculateMoveScoreForObjective(BattleController BC, Unit actor) {
		switch (strategem.objectiveType) {
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
				List<Awareness> awarenesses = BC.awarenessController.TopAwarenesses(actor);
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

				/// Add point if tile is along a foe's path
				if (pathsToExit.Contains(moveLocation)) {
					_description += "Move target is on a foe's path to the exit. +1 / ";
					_score++;
				}

				/// Rate each tile based on its distance from the exit!!!!
				int movementRange = actor.GetComponent<Stats>()[StatTypes.MOV];
				Point distance = nearestExitMarkerTile.pos - moveLocation.pos;
				int distanceRaw = Mathf.Abs(distance.x) + Mathf.Abs(distance.y);
				int differenceBetweenRangeAndDistance = movementRange - distanceRaw;
				_score += differenceBetweenRangeAndDistance;
				_description += "Difference between range and distance from exit is " + differenceBetweenRangeAndDistance.ToString();
				_description += ". " + (differenceBetweenRangeAndDistance < 0? "": "+") + differenceBetweenRangeAndDistance.ToString() + ". / ";
				break;
			case ObjectiveType.ProtectSelf:
				// Among the existing tiles, find the ones furthest from all known foes
				break;
			default:
				break;
		}
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
	void RateFireLocation (BattleController BC, Unit actor) {
		AbilityArea area = ability.GetComponent<AbilityArea>();
		List<Tile> tiles = area.GetTilesInArea(BC.board, fireLocation.pos);

		int fireLocationBonus = 0;
		for (int i = 0; i < tiles.Count; ++i) {
			Tile tile = tiles[i];
			if (actor.tile == tiles[i] || !ability.IsTarget(tile))
				continue;
			
			fireLocationBonus += CalculateScoreForTargetMatch(BC, actor, tile);
		}
		_description += "fireLocationBonus is " + fireLocationBonus.ToString() + ".";
		if (fireLocationBonus <= 0) {
			_description += " No targets. Minimizing score and removing ability / ";
			_score -= 1000;
			ability = null;
			fireLocation = null;
		} else {
			_description += " / ";
			_score += fireLocationBonus;
		}
	}

	/* This method shows how to determine which marks are a match or not.
	 * An ability which targets a tile is simply marked as true
	 * (I havent actually implemented any such abilities, so I might change this logic later).
	 * Otherwise, I use the alliance component to determine whether or not the target type is a match. */
	int CalculateScoreForTargetMatch(BattleController BC, Unit actor, Tile tile) {
		bool isMatch = false;
		if (targetType == TargetType.Tile) {
			isMatch = true;
			_description += tile.ToString() + " is a target match. (target is tile) / ";
		} else if (targetType != TargetType.None) {
			Alliance actorAlliance = actor.GetComponent<Alliance>();
			Alliance targetAlliance = tile.occupant.GetComponentInChildren<Alliance>();
			Unit targetUnit = targetAlliance.GetComponent<Unit>();
			if (targetAlliance != null) {
				if (actorAlliance.IsMatch(targetAlliance, targetType)) {
					if (actorAlliance == targetAlliance &&
					  (targetType == TargetType.Self || targetType == TargetType.AllyOrSelf)) {
						isMatch = true;
						_description += tile.ToString() + " is a target match. (target type includes self) / ";
					} else if (targetType == TargetType.Ally) {
						// Allies are assumed to have automatic knowledge of each other's location
						isMatch = true;
						_description += tile.ToString() + " is a target match. (target is ally) / ";
					} else {
						// If the target is a foe who has been seen by the actor, it's viable
						isMatch = BC.awarenessController.IsAwareOfUnit(actor, targetUnit, new AwarenessType[] {AwarenessType.Seen});
						if (isMatch) {
							_description += tile.ToString() + " is a target match. (target was seen by actor) / ";
						}
					}
				}
			}
		}
		return CalculateAngleBasedScore(actor, tile, isMatch);
	}

	void CalculateScoreForPosition(BattleController BC, Unit actor) {
		if (IsAbilityAngleBased(ability)) {
			Tile startTile = actor.tile;
			Direction startDirection = actor.dir;
			actor.dir = attackDirection;

			actor.Place(moveLocation);

			if (BC.cpu.IsMissileImpeded(actor, ability, fireLocation)) {
				_score--;
				_description += "Missle is impeded by wall or other unit. -1 / ";
			}
			
			// Increase the score if the unit doesn't have to move.
			if (moveLocation == startTile) {
				_score++;
				_description += "Unit does not have to move. +1 / ";
			}
			
			actor.Place(startTile);
			actor.dir = startDirection;;
		}
	}

	bool IsAbilityAngleBased (Ability ability) {
		bool isAngleBased = false;
		for (int i = 0; i < ability.transform.childCount; ++i) {
			HitRate hr = ability.transform.GetChild(i).GetComponent<HitRate>();
			if (hr.IsAngleBased) {
				isAngleBased = true;
				break;
			}
		}
		return isAngleBased;
	}

	int CalculateAngleBasedScore (Unit actor, Tile tile, bool isMatch) {
		Tile startTile = actor.tile;
		Direction startDirection = actor.dir;
		actor.dir = attackDirection;

		int value = isMatch ? 1 : -1;
		int multiplier = MultiplierForAngle(actor, tile);
		int angleBasedBonus = value * multiplier;

		actor.Place(startTile);
		actor.dir = startDirection;;

		return angleBasedBonus;
	}

	/// Right now the below is overriden until we decide who we want to handle facing.
	/* This method helps to score move target options based on the angle of attack.
	 * I could have picked any number I wanted to balance the preferences,
	 * but I chose numbers which currently match the general percent chance of hitting from that angle.
	 * This means that in general I favor attacks from behind,
	 * but having a chance to hit two units from the front could still be better than targeting one unit from behind. */
	int MultiplierForAngle (Unit caster, Tile tile) {
		if (tile.occupant == null)
			return 0;

		// Unit defender = tile.occupant.GetComponentInChildren<Unit>();
		// if (defender == null)
		// 	return 0;

		// Facings facing = caster.GetFacing(defender);
		// if (facing == Facings.Back)
		// 	return 3;
		// if (facing == Facings.Side)
		// 	return 2;
		return 1;
	}
}
