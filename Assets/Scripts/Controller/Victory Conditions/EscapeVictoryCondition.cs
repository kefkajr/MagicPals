using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EscapeVictoryCondition : BaseVictoryCondition
{	

    protected override void OnEnable ()
	{
		base.OnEnable();
		this.AddObserver(CheckForHeroEscape, AwarenessController.NotficationEscape);
	}

	protected override void OnDisable ()
	{
		base.OnDisable();
		this.RemoveObserver(CheckForHeroEscape, AwarenessController.NotficationEscape);
	}

	protected override void CheckForGameOver ()
	{
		// If any Ally is defeated, Game Over
		for (int i = 0; i < bc.units.Count; ++i)
		{
			Unit unit = bc.units[i];
			Alliance a = unit.GetComponent<Alliance>();
			if (a == null)
				continue;
			
			if (a.type == Alliances.Hero && unit.IsDefeated())
				Victor = Alliances.Enemy;
		}
	}

    void CheckForHeroEscape(object sender, object args)
	{
        bool isAnyPartyMemberOnField = bc.units.Where(unit => unit.GetComponent<Alliance>() != null)
                                               .Where(unit => unit.GetComponent<Alliance>().type == Alliances.Hero).Count() > 0;
        bool hasAnyPartyMemberEscaped = bc.escapedUnits.Where(unit => unit.GetComponent<Alliance>() != null)
                                                       .Where(unit => unit.GetComponent<Alliance>().type == Alliances.Hero).Count() > 0;
		if (!isAnyPartyMemberOnField && hasAnyPartyMemberEscaped) {
			Victor = Alliances.Hero;
		}
	}
}
