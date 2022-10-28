using UnityEngine;
using System.Collections;

/* This abstract base class indicates that we simply want
 * to be able to pick an ability to use in a turn,
 * but it doesn’t care how or why the ability is chosen.
 * The abstract Pick method must be implemented by concrete subclasses
 * and in that step will update the “Plan of Attack” object
 * which had been passed along as a parameter. */

public abstract class BaseAbilityPicker : MonoBehaviour
{
	#region Fields
	protected Unit owner;
	protected AbilityCatalog ac;
	#endregion

	#region MonoBehaviour
	void Start ()
	{
		owner = GetComponentInParent<Unit>();
		ac = owner.GetComponentInChildren<AbilityCatalog>();
	}
	#endregion

	#region Public
	public abstract void Pick (PlanOfAttack plan);
	#endregion

	#region Protected

	/* For convenience sake, we added a method which can find
	 * an ability which matches a given name (as a string).
	 * This was important since our units are created dynamically
	 * and we can’t directly link from the picker to the ability
	 * since it wont have been created yet. */
	protected Ability Find (string abilityName)
	{
		for (int i = 0; i < ac.transform.childCount; ++i)
		{
			Transform category = ac.transform.GetChild(i);
			Transform child = category.Find(abilityName);
			if (child != null)
				return child.GetComponent<Ability>();
		}
		return null;
	}

	protected Ability Default ()
	{
		return owner.GetComponentInChildren<Ability>();
	}
	#endregion
}