using UnityEngine;
using System.Collections;

public class MatchAdjustment : BaseAdjustment {
	public readonly Unit attacker;
	public readonly Unit target;

	public MatchAdjustment (Unit attacker, Unit target) : base (false) {
		this.attacker = attacker;
		this.target = target;
	}
}
