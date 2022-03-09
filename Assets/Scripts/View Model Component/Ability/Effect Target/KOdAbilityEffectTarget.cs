using UnityEngine;
using System.Collections;

public class KOdAbilityEffectTarget : AbilityEffectTarget 
{
	public override bool IsTarget (Tile tile)
	{
		if (tile == null || tile.occupant == null)
			return false;

		Stats s = tile.occupant.GetComponent<Stats>();
		return s != null && s[StatTypes.HP] <= 0;
	}
}