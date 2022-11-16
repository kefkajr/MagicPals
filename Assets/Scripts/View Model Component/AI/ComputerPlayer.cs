using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ComputerPlayer : MonoBehaviour 
{
	#region Fields
	BattleController bc;
	Unit actor { get { return bc.turn.actor; }}
	Alliance alliance { get { return actor.GetComponent<Alliance>(); }}
	Perception perception { get { return actor.GetComponent<Perception>(); } }
	Unit nearestFoe;
	#endregion
	
	#region MonoBehaviour
	void Awake ()
	{
		bc = GetComponent<BattleController>();
	}
	#endregion

	#region Public
	// Create and fill out a plan of attack
	public PlanOfAttack Evaluate ()
	{
		PlanOfAttack poa = new PlanOfAttack();

		// Step 1: Decide what ability to use
		AttackPattern pattern = actor.GetComponentInChildren<AttackPattern>();
		if (pattern)
			pattern.Pick(poa);
		else
			DefaultAttackPattern(poa);

		Debug.Log(string.Format("{0} wants to use {1}", actor.name, poa.ability.name));

		// Step 2: Determine where to move and aim to best use the ability
		if (IsPositionIndependent(poa))
			// It doesn't matter where you stand
			PlanPositionIndependent(poa);
		else if (IsDirectionIndependent(poa))
			// It DOES matter where you stand, but it doesn't matter where you face
			PlanDirectionIndependent(poa);
		else
			// It DOES matter where you stand and it DOES matter where you face
			PlanDirectionDependent(poa);

		if (poa.ability == null)
		{
			Debug.Log(string.Format("{0} can't use an ability, so will try to investigate", actor.name));

			// Just position yourself better for the next turn
			Investigate(poa);
		}

		// Step 3: Return the completed plan
		return poa;
	}
	#endregion
	
	#region Private
	void DefaultAttackPattern (PlanOfAttack poa)
	{
		// Just get the first "Attack" ability
		poa.ability = actor.GetComponentInChildren<Ability>();
		poa.target = Targets.Foe;
	}

	bool IsPositionIndependent (PlanOfAttack poa)
	{
		AbilityRange range = poa.ability.GetComponent<AbilityRange>();
		return range.positionOriented == false;
	}
	
	bool IsDirectionIndependent (PlanOfAttack poa)
	{
		AbilityRange range = poa.ability.GetComponent<AbilityRange>();
		return !range.directionOriented;
	}

	/* When it is determined that an ability is position independent,
	 * then I simply move to a random tile within the unit’s move range, because position didn’t matter.
	 * There is room to polish this up –
	 * perhaps the unit would rather move toward or away from its foesduring this time.
	 * If you want more specific behavior like that it shouldn’t be hard to add.
	 * I’ll show an example of moving toward the nearest foe soon. */
	void PlanPositionIndependent (PlanOfAttack poa)
	{
		List<Tile> moveOptions = GetMoveOptions();
		// TODO: Have the unit move somewhere logical, instead of moving randomly
		Tile tile = moveOptions[Random.Range(0, moveOptions.Count - 1)];
		poa.moveLocation = poa.fireLocation = tile.pos;
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
	void PlanDirectionIndependent (PlanOfAttack poa)
	{
		Tile startTile = actor.tile;
		Dictionary<Tile, AttackOption> attackOptionsByTile = new Dictionary<Tile, AttackOption>();
		AbilityRange ar = poa.ability.GetComponent<AbilityRange>();
		List<Tile> moveOptions = GetMoveOptions();
		
		for (int i = 0; i < moveOptions.Count; ++i)
		{
			Tile moveTile = moveOptions[i];
			actor.Place( moveTile );
			List<Tile> fireOptions = ar.GetTilesInRange(bc.board);
			
			for (int j = 0; j < fireOptions.Count; ++j)
			{
				Tile fireTile = fireOptions[j];
				AttackOption attackOption = null;
				if (attackOptionsByTile.ContainsKey(fireTile))
				{
					attackOption = attackOptionsByTile[fireTile];
				}
				else
				{
					attackOption = new AttackOption();
					attackOptionsByTile[fireTile] = attackOption;
					attackOption.target = fireTile;
					attackOption.direction = actor.dir;
					RateFireLocation(poa, attackOption);
				}

				attackOption.AddMoveTarget(moveTile);
			}
		}
		
		actor.Place(startTile);
		List<AttackOption> attackOptions = new List<AttackOption>(attackOptionsByTile.Values);
		PickBestOption(poa, attackOptions);
	}

	/* This last case depends both on a unit’s position on the board
	 * and the direction the unit faces while using the selected ability.
	 * It should look pretty similar to the “PlanDirectionIndependent” variation.
	 * The main difference here is that instead of grabbing the Ability Range component
	 * and looping through targeted tiles, we instead loop through each of the four facing directions.
	 * Every single entry generated will have a unique area of effect –
	 * there is no overlap or need for the dictionary as I had last time.
	 * We can simply track each entry in a list directly. */
	void PlanDirectionDependent (PlanOfAttack poa)
	{
		Tile startTile = actor.tile;
		Directions startDirection = actor.dir;
		List<AttackOption> attackOptions = new List<AttackOption>();
		List<Tile> moveOptions = GetMoveOptions();
		
		for (int i = 0; i < moveOptions.Count; ++i)
		{
			Tile moveTile = moveOptions[i];
			actor.Place( moveTile );
			
			for (int ii = 0; ii < 4; ++ii)
			{
				actor.dir = (Directions)ii;
				AttackOption attackOption = new AttackOption();
				attackOption.target = moveTile;
				attackOption.direction = actor.dir;
				RateFireLocation(poa, attackOption);
				attackOption.AddMoveTarget(moveTile);
				attackOptions.Add(attackOption);
			}
		}
		
		actor.Place(startTile);
		actor.dir = startDirection;
		PickBestOption(poa, attackOptions);
	}
	
	List<Tile> GetMoveOptions ()
	{
		return actor.GetComponent<Movement>().GetTilesInRange(bc.board);
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
	void RateFireLocation (PlanOfAttack poa, AttackOption option)
	{
		AbilityArea area = poa.ability.GetComponent<AbilityArea>();
		List<Tile> tiles = area.GetTilesInArea(bc.board, option.target.pos);
		option.areaTargets = tiles;
		option.isCasterMatch = IsAbilityTargetMatch(poa, actor.tile);

		for (int i = 0; i < tiles.Count; ++i)
		{
			Tile tile = tiles[i];
			if (actor.tile == tiles[i] || !poa.ability.IsTarget(tile))
				continue;
			
			bool isMatch = IsAbilityTargetMatch(poa, tile);
			option.AddMark(tile, isMatch);
		}
	}

	/* This method shows how to determine which marks are a match or not.
	 * An ability which targets a tile is simply marked as true
	 * (I havent actually implemented any such abilities, so I might change this logic later).
	 * Otherwise, I use the alliance component to determine whether or not the target type is a match. */
	bool IsAbilityTargetMatch(PlanOfAttack poa, Tile tile)
	{
		bool isMatch = false;
		if (poa.target == Targets.Tile)
			isMatch = true;
		else if (poa.target != Targets.None)
		{
			Alliance other = tile.occupant.GetComponentInChildren<Alliance>();
			Unit unit = other.GetComponent<Unit>();
			if (other != null && perception.IsAwareOfUnit(unit, AwarenessType.Seen) && alliance.IsMatch(other, poa.target))
				isMatch = true;
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
	void PickBestOption (PlanOfAttack poa, List<AttackOption> options)
	{
		int bestScore = 1;
		List<AttackOption> bestOptions = new List<AttackOption>();
		for (int i = 0; i < options.Count; ++i)
		{
			AttackOption option = options[i];
			int score = option.GetScore(actor, poa.ability);
			if (score > bestScore)
			{
				bestScore = score;
				bestOptions.Clear();
				bestOptions.Add(option);
			}
			else if (score == bestScore)
			{
				bestOptions.Add(option);
			}
		}

		if (bestOptions.Count == 0)
		{
			poa.ability = null; // Clear ability as a sign not to perform it
			return;
		}

		List<AttackOption> finalPicks = new List<AttackOption>();
		bestScore = 0;
		for (int i = 0; i < bestOptions.Count; ++i)
		{
			AttackOption option = bestOptions[i];
			int score = option.bestAngleBasedScore;
			if (score > bestScore)
			{
				bestScore = score;
				finalPicks.Clear();
				finalPicks.Add(option);
			}
			else if (score == bestScore)
			{
				finalPicks.Add(option);
			}
		}
		
		AttackOption choice = finalPicks[ UnityEngine.Random.Range(0, finalPicks.Count)  ];
		poa.fireLocation = choice.target.pos;
		poa.attackDirection = choice.direction;
		poa.moveLocation = choice.bestMoveTile.pos;
	}

	/* There are a few different times in a turn when I might want to know where the nearest foe is to a given unit.
	 * For example, whenever the turn had no good targets to use the selected ability on
	 * – it assumes that the reason is that it wasn’t close enough to the enemy,
	 * so I figure out where the nearest foe is and attempt to move toward it.
	 * I determine this by using the board’s search method and passing along an anonymous delegate.
	 * If it finds a tile with a unit that is a Foe (according to the alliance component)
	 * and also isn’t KO’d [AND ALSO HAS BEEN SEEN], then I have found a target worth moving toward. */
	void FindNearestFoe ()
	{
		nearestFoe = null;
		bc.board.Search(actor.tile, delegate(Tile arg1, Tile arg2) {
			if (nearestFoe == null && arg2.occupant != null)
			{
				Alliance other = arg2.occupant.GetComponentInChildren<Alliance>();
				if (other != null && alliance.IsMatch(other, Targets.Foe))
				{
					Unit unit = other.GetComponent<Unit>();
					Stats stats = unit.GetComponent<Stats>();

					// If target is alive and the actor has seen them.
					if (stats[StatTypes.HP] > 0 && perception.IsAwareOfUnit(unit, AwarenessType.Seen))
					{
						nearestFoe = unit;
						return true;
					}
				}
			}
			return nearestFoe == null;
		});
	}

	// TODO: If the new "Investigate" method doesn't work out, this should be deprecated.
	/* Whenever a board search has been run, there will be tracking data left over in the tiles.
	 * I can iterate over the “path” in reverse until I find a tile which happens to be included in the movement range of the unit.
	 * This will allow me to move as close as possible to the foe who is nearest. */
	void MoveTowardOpponent (PlanOfAttack poa)
	{
		List<Tile> moveOptions = GetMoveOptions();
		FindNearestFoe();
		if (nearestFoe != null)
		{
			Tile toCheck = nearestFoe.tile;
			while (toCheck != null)
			{
				if (moveOptions.Contains(toCheck))
				{
					poa.moveLocation = toCheck.pos;
					return;
				}
				toCheck = toCheck.prev;
			}
		}

		poa.moveLocation = actor.tile.pos;
	}


	// NEW Investigation Methods

	void Investigate(PlanOfAttack poa)
	{
		List<Tile> moveOptions = GetMoveOptions();
		FindTopAwarenessFoeOnBoard(shouldCheckSeenOnly: false);
		if (nearestFoe != null)
		{
			List<Tile> idealPath = bc.board.FindPath(actor.tile, nearestFoe.tile);
			Debug.Log(string.Format("{0} is investigating {1}", actor.name, nearestFoe.name));
			Tile toCheck = idealPath.Count > 0 ? idealPath.Last() : null;
			while (toCheck != null)
			{
				if (moveOptions.Contains(toCheck))
				{
					// Move toward top awareness / point of interest
					poa.moveLocation = toCheck.pos;
					return;
				}
				// Board search keeps previous tiles in memory
				toCheck = toCheck.prev;
			}
		}

		// Stay put
		// TODO: Perform sentry duties instead
		poa.moveLocation = actor.tile.pos;
	}

	void FindTopAwarenessFoeOnBoard(bool shouldCheckSeenOnly)
	{
		nearestFoe = null;
		List<Awareness> topAwarenesses = perception.TopAwarenesses().FindAll( delegate (Awareness a) {
			Alliance otherAlliance = a.stealth.unit.GetComponentInChildren<Alliance>();
			return alliance.IsMatch(otherAlliance, Targets.Foe);
		});
		if (topAwarenesses.Count == 0) return;

		Unit targetUnit = topAwarenesses[0].stealth.GetComponent<Unit>();
		bc.board.Search(actor.tile, delegate (Tile arg1, Tile arg2) {
			if (nearestFoe == null && arg2.occupant != null)
			{
				Unit foundUnit = arg2.occupant.GetComponent<Unit>();
				if (targetUnit == foundUnit)
				{
					if (shouldCheckSeenOnly && !perception.IsAwareOfUnit(foundUnit, AwarenessType.Seen))
                    {
						return false;
                    }

					nearestFoe = targetUnit;
					return true;
				}
			}
			return nearestFoe == null;
		});
	}

	/* After we have moved and used an ability, we need to determine an end facing direction.
	 * For this I find the nearest foe again. Note that it is important to do this a second time,
	 * because the foe who was nearest before you moved is not necessarily the foe who is nearest after you have moved.
	 * Next I loop through each of the directions until I find a direction which has me face the foe from the front.
	 * This way the foe is less likely to be able to attack me from the back. */
	public Directions DetermineEndFacingDirection ()
	{
		Directions dir = (Directions)UnityEngine.Random.Range(0, 4);
		FindTopAwarenessFoeOnBoard(shouldCheckSeenOnly: false);
		if (nearestFoe != null)
		{
			Directions start = actor.dir;
			for (int i = 0; i < 4; ++i)
			{
				actor.dir = (Directions)i;
				if (nearestFoe.GetFacing(actor) == Facings.Front)
				{
					dir = actor.dir;
					break;
				}
			}
			actor.dir = start;
		}
		return dir;
	}
	#endregion
}