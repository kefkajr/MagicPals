﻿using UnityEngine;
using System.Collections;

public class UndeadAbilityEffectTarget : AbilityEffectTarget 
{
	/// <summary>
	/// Indicates whether the Undead component must be present (true)
	/// or must not be present (false) for the target to be valid.
	/// </summary>
	public bool toggle;

	public override bool IsTarget (Tile tile)
	{
		if (tile == null || tile.occupant == null)
			return false;

		bool hasComponent = tile.occupant.GetComponent<Undead>() != null;
		if (hasComponent != toggle)
			return false;
		
		Stats s = tile.occupant.GetComponent<Stats>();
		return s != null && s[StatTypes.HP] > 0;
	}
}
