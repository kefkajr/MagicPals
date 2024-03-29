﻿using UnityEngine;
using System.Collections;

public class DefeatTargetVictoryCondition : BaseVictoryCondition {
	public Unit target;
	
	protected override void CheckForGameOver() {
		base.CheckForGameOver ();
		if (Victor == Alliances.None && target.IsDefeated())
			Victor = Alliances.Hero;
	}
}
