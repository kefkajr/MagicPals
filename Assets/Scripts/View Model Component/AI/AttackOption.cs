using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* The “notes” I take on any given location from which to evaluate the usage of an ability is also a somewhat lengthy class.
 * A lot of its functionality I talked through while covering the ComputerPlayer script, but just to be clear I will break it down as well. */
public class AttackOption 
{
	#region Classes
	/* I created another class inside of AttackOption called Mark to hold a pair of data.
	 * This is a little cleaner and less error prone than maintaining two separate lists
	 * (one for the tiles and one for whether or not the tile was a match).
	 * The Mark class is private to the implementation of the AttackOption class,
	 * and is only used for convenience and readability.
	 * If other classes needed to know about it or use it,
	 * then I would probably stick it in its own file. */
	public class Mark
	{
		public Tile tile;
		public bool isMatch;
		
		public Mark (Tile tile, bool isMatch)
		{
			this.tile = tile;
			this.isMatch = isMatch;
		}
	}
	#endregion

	#region Fields
	/* There are several fields to fill out here.
	 * The “target” and “direction” fields are treated slightly differently (or not at all) based on the type of ability being used.
	 * For example, “target” would represent either the tile which we highlighted to use as a firing location, 
	 * or the tile we would want to move to in order to fire,
	 * and direction may or may not apply. */
	public Tile target;
	public Direction direction;

	/* The area targets are the list of tiles which fall within an ability’s area of effect,
	 * regardless of if they currently have a target or not.
	 * This can be important in cases where I had overlap
	 * because I might initially “score” an attack based on the user being in one position,
	 * but could actually “pick” an alternate move location later.
	 * If the ability were intended for foes,
	 * then I would want to make sure I didn’t pick an option where the ability would also hit the caster.
	 * On the other hand, if the ability is intended for allies,
	 * then allowing the abilty to include the caster would be a nice bonus. */
	public List<Tile> areaTargets = new List<Tile>();

	// The “isCasterMatch” field tracks whether or not one of the move target locations would actually be good to move into or not.
	public bool isCasterMatch;

	/* The “bestMoveTile” and “bestAngleBasedScore” fields indicate a tile which provides the best score to attack from.
	 * This score would be based on the idea that attacking from behind gives a greater chance of a hit actually connecting.
	 * Attacks from the front are easier for an enemy to dodge, so we want to naturally pick ones from the rear. */
	public Tile bestMoveTile { get; private set; }
	public int bestAngleBasedScore { get; private set; }

	// The list of marks grows based on the number of legal targets which are
    // within the area of effect for an ability given the target tile and facing direction.
	public List<Mark> marks = new List<Mark>();

	// The list of move targets indicates which locations the unit can move to in order to cast at the indicated target location.
    // Direction oriented abilties will only ever have one move target in this list.
	List<Tile> moveTargets = new List<Tile>();
	#endregion

	#region Public
	/* The AddMoveTarget method is called to build up the list of locations which are in firing range of the current tile.
	 * Note that I don’t actually include options that would be bad for the caster,
	 * for example I wouldn’t want to move within the blast radius of my own attack. */
	public void AddMoveTarget (Tile tile)
	{
		// Dont allow moving to a tile that would negatively affect the caster
		if (!isCasterMatch && areaTargets.Contains(tile))
			return;
		moveTargets.Add(tile);
	}

	/* The AddMark method creates an instance of the class we defined above and adds it to a list.
	 * Remember that a mark indicates a target (good or bad) can be hit by whatever fire location was chosen for this AttackOption instance. */
	public void AddMark (Tile tile, bool isMatch)
	{
		marks.Add (new Mark(tile, isMatch));
	}

	// Scores the option based on how many of the targets are of the desired type
	/* Here we will provide a score by which we can sort the various options available during a turn.
	 * Before calculating a score it also looks for the best tile to move to in order to use the ability.
	 * The best move target will be the one which has the best score based on the second factor (the angle of attack)
	 * – such as attacking a unit from behind gives more points than attacking from the front.
	 * In the event that there are no good locations,
	 * then the overall score is returned early as zero.
	 * 
	 * Assuming that a good move tile is found,
	 * the score is then tallied based on how many of the marks are a match or not.
	 * If the caster is a match for the ability then an extra point is awarded,
	 * because we can potentially move to a location where the ability will include it.*/
	public int GetScore (Unit caster, Ability ability)
	{
		GetBestMoveTarget(caster, ability);
		if (bestMoveTile == null)
			return 0;

		int score = 0;
		for (int i = 0; i < marks.Count; ++i)
		{
			if (marks[i].isMatch)
				score++;
			else
				score--;
		}

		if (isCasterMatch && areaTargets.Contains(bestMoveTile))
			score++;

		return score;
	}
	#endregion

	#region Private
	// Returns the tile which is the most effective point for the caster to attack from
	/* There are two main types of abilities to consider,
	 * ones which the angle from the attacker to the target makes a difference and ones which do not care about the angle.
	 * For the abilities where angle does matter,
	 * we use a simple algorithm that looks a lot like what you have seen a few times now
	 * in the Computer Player script where we sort attack options.
	 * The basic idea is that we loop through all of the tiles we can move to, move the unit there,
	 * calculate the angles between the caster and the targets and use those angles to generate a score.
	 * When a score is greater than the previous best score, we reset the list of best options,
	 * and when we have considered all options, we pick at random from tiles with the highest score.
	 * When the angle is irrelevant, we can simply return any tile at random. */
	void GetBestMoveTarget (Unit caster, Ability ability)
	{
		if (moveTargets.Count == 0)
			return;
		
		if (IsAbilityAngleBased(ability))
		{
			bestAngleBasedScore = int.MinValue;
			Tile startTile = caster.tile;
			Direction startDirection = caster.dir;
			caster.dir = direction;

			List<Tile> bestOptions = new List<Tile>();
			for (int i = 0; i < moveTargets.Count; ++i)
			{
				caster.Place(moveTargets[i]);
				int score = GetAngleBasedScore(caster);
				if (score > bestAngleBasedScore)
				{
					bestAngleBasedScore = score;
					bestOptions.Clear();
				}

				if (score == bestAngleBasedScore)
				{
					bestOptions.Add(moveTargets[i]);
				}
			}
			
			caster.Place(startTile);
			caster.dir = startDirection;

			FilterBestMoves(bestOptions);
			bestMoveTile = bestOptions[ UnityEngine.Random.Range(0, bestOptions.Count) ];
		}
		else
		{
			bestMoveTile = moveTargets[ UnityEngine.Random.Range(0, moveTargets.Count) ];
		}
	}

	// Indicates whether the angle of attack is an important factor in the
	// application of this ability
	/* Here we get the angle from the caster to each of the mark locations.
	 * The resulting score is either incremented or decremented
	 * based on whether or not the mark was an intended match.
	 * The amount the score goes up or down is based on the angle (front, side or back). */
	bool IsAbilityAngleBased (Ability ability)
	{
		bool isAngleBased = false;
		for (int i = 0; i < ability.transform.childCount; ++i)
		{
			HitRate hr = ability.transform.GetChild(i).GetComponent<HitRate>();
			if (hr.IsAngleBased)
			{
				isAngleBased = true;
				break;
			}
		}
		return isAngleBased;
	}

	// Scores the option based on how many of the targets are a match
	// and considers the angle of attack to each mark
	int GetAngleBasedScore (Unit caster)
	{
		int score = 0;
		for (int i = 0; i < marks.Count; ++i)
		{
			int value = marks[i].isMatch ? 1 : -1;
			int multiplier = MultiplierForAngle(caster, marks[i].tile);
			score += value * multiplier;
		}
		return score;
	}

	/* After we have built a list of the best places to move to
	 * (scored based on the number of good marks and the angle based scores)
	 * we can potentially further optimize the remaining selection.
	 * If the caster is not a match
	 * (such as when we are using an offensive ability intended for an opponent)
	 * then we wont do anything.
	 * Then we need to decide whether or not the caster can move to one of the locations
	 * which is also included in the area of effect.
	 * If so, we remove any tile from the best choices list which isn’t that good. */
	void FilterBestMoves (List<Tile> list)
	{
		if (!isCasterMatch)
			return;

		bool canTargetSelf = false;
		for (int i = 0; i < list.Count; ++i)
		{
			if (areaTargets.Contains(list[i]))
			{
				canTargetSelf = true;
				break;
			}
		}

		if (canTargetSelf)
		{
			for (int i = list.Count - 1; i >= 0; --i)
			{
				if (!areaTargets.Contains(list[i]))
					list.RemoveAt(i);
			}
		}
	}

	/* This method helps to score move target options based on the angle of attack.
	 * I could have picked any number I wanted to balance the preferences,
	 * but I chose numbers which currently match the general percent chance of hitting from that angle.
	 * This means that in general I favor attacks from behind,
	 * but having a chance to hit two units from the front could still be better than targeting one unit from behind. */
	int MultiplierForAngle (Unit caster, Tile tile)
	{
		if (tile.occupant == null)
			return 0;

		Unit defender = tile.occupant.GetComponentInChildren<Unit>();
		if (defender == null)
			return 0;

		Facings facing = caster.GetFacing(defender);
		if (facing == Facings.Back)
			return 90;
		if (facing == Facings.Side)
			return 75;
		return 50;
	}
	#endregion
}