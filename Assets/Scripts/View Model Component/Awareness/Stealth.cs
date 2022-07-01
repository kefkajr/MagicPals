using UnityEngine;
using System.Collections;

public class Stealth : MonoBehaviour
{
	public Alliance disguisedAlliance; // The alliance that their current disguise presents (likely should be repalce by a separate DISGUISE type
	public bool isCrouching; // Should make it harder to be seen, barring certain conditions
	public bool isInvisible; // Should make it impossible to be seen, barring certain conditions

	public Unit unit { get { return GetComponentInParent<Unit>(); } }
	public Alliance trueAlliance { get { return unit.GetComponent<Alliance>(); } } // The default alliance of the unit

}