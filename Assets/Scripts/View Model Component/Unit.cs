using UnityEngine;
using System.Collections;

public class Unit : MonoBehaviour 
{
	public Tile tile { get; protected set; }
	public Direction dir;

	public Perception perception { get { return GetComponentInChildren<Perception>(); } }
	public Stealth stealth { get { return GetComponent<Stealth>(); } }

	public KnockOutStatusEffect KO { get { return GetComponentInChildren<KnockOutStatusEffect>(); } }

	public void Place (Tile target)
	{
		// Make sure old tile location is not still pointing to this unit
		if (tile != null && tile.occupant == gameObject)
			tile.occupant = null;
		
		// Link unit and tile references
		tile = target;
		
		if (target != null)
			target.occupant = gameObject;
	}

	public void Match ()
	{
		transform.localPosition = tile.center;
		transform.localEulerAngles = dir.ToEuler();
	}

	public bool IsDefeated ()
	{
		Health health = GetComponent<Health>();
		if (health)
			return health.MinHP == health.HP;
		
		Stats stats = GetComponent<Stats>();
		return stats[StatTypes.HP] == 0;
	}

}