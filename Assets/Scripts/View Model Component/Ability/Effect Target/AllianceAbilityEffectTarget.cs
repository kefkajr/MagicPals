using UnityEngine;
using System.Collections;

public class AllianceAbilityEffectTarget : AbilityEffectTarget 
{
	public TargetType targetType;
	Alliance alliance;

	void Start ()
	{
		alliance = GetComponentInParent<Alliance>();
	}

	public override bool IsTarget (Tile tile)
	{
		if (tile == null || tile.occupant == null)
			return false;

		Alliance other = tile.occupant.GetComponentInChildren<Alliance>();
		return alliance.IsMatch(other, targetType);
	}
}