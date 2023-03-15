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
