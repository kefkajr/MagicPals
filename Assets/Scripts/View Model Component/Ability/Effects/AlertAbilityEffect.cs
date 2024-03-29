using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class AlertAbilityEffect : BaseAbilityEffect {

	public override int Predict(Tile target) {
		return 0;
	}

	protected override int OnApply(Tile target) {
		Unit alertingUnit = GetComponentInParent<Unit>();
		Unit alertedUnit = target.occupant.GetComponent<Unit>();

		if (alertingUnit == alertedUnit)
			return 0;

		AwarenessController ac = GameObject.Find("Awareness Controller").GetComponent<AwarenessController>();
		List<Awareness> awarenesses = ac.TopAwarenesses(alertingUnit);

		foreach (Awareness awareness in awarenesses) {
			// If the alerting unit shouts, alerted unit should become aware of the same units,
            // but that doesn't mean they actually see them.
			AwarenessType newType = awareness.type != AwarenessType.Seen ? awareness.type : AwarenessType.MayHaveSeen;

			if (alertedUnit != awareness.stealth.unit) {

				// We also don't want to overwrite the alerted unit's existing awareness,
				// only upgrade it if the level is higher.
				// That is, we don't want alerted units to "unsee" units they currently see.
				AwarenessType oldType = ac.awarenessMap[alertedUnit][awareness.stealth.unit].type;

				if ((int)newType > (int)oldType) {
					// Give the alert united the same type of awareness of the target as the alerting unit (except for seeing)
					var awarenessToChange = ac.awarenessMap[alertedUnit][awareness.stealth.unit];
					ac.UpdateAwareness(awarenessToChange, newType, awareness.pointOfInterest);
				}

			}
		}

		return 0;
	}
}