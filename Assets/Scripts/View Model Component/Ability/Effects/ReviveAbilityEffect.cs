using UnityEngine;
using System.Collections;

public class ReviveAbilityEffect : BaseAbilityEffect 
{
	public float percent;

	public override int Predict (Tile target)
	{
		Stats s = target.occupant.GetComponent<Stats>();
		return Mathf.FloorToInt(s[StatTypes.MHP] * percent);
	}

	protected override int OnApply (Tile target)
	{
		Stats s = target.occupant.GetComponent<Stats>();

		int value = Predict(target);
		s.SetValue(StatTypes.HP, value, true);
		return value;
	}
}