using UnityEngine;
using System.Collections;

public class HealAbilityEffect : BaseAbilityEffect 
{
	public string constantValue = null;

	public override int Predict (Tile target)
	{
		if (constantValue != null && constantValue != "")
			return int.Parse(constantValue);

		Unit attacker = GetComponentInParent<Unit>();
		Unit defender = target.occupant.GetComponent<Unit>();
		return GetStat(attacker, defender, GetPowerNotification, 0);
	}

	protected override int OnApply (Tile target)
	{
		Unit defender = target.occupant.GetComponent<Unit>();
		int value;
		if (constantValue == null || constantValue == "")
		{
			// Start with the predicted value
			value = Predict(target);

			// Add some random variance
			value = Mathf.FloorToInt(value * UnityEngine.Random.Range(0.9f, 1.1f));

			// Clamp the amount to a range
			value = Mathf.Clamp(value, minDamage, maxDamage);
		} else
        {
			// Use constant value
			value = int.Parse(constantValue);
        }

		// Apply the amount to the target
		Stats s = defender.GetComponent<Stats>();
		s.SetValue(StatTypes.HP, value, true);
		return value;
	}
}