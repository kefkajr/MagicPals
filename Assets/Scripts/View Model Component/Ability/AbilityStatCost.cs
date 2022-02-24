using UnityEngine;
using System.Collections;

public class AbilityStatCost : MonoBehaviour 
{
	#region Fields
	public StatTypes type = StatTypes.MP;
	public int amount;
	Ability owner;
	#endregion

	#region MonoBehaviour
	void Awake ()
	{
		owner = GetComponent<Ability>();
	}

	void OnEnable ()
	{
		this.AddObserver(OnCanPerformCheck, Ability.CanPerformCheck, owner);
		this.AddObserver(OnDidPerformNotification, Ability.DidPerformNotification, owner);
	}

	void OnDisable ()
	{
		this.RemoveObserver(OnCanPerformCheck, Ability.CanPerformCheck, owner);
		this.RemoveObserver(OnDidPerformNotification, Ability.DidPerformNotification, owner);
	}
	#endregion

	#region Notification Handlers
	void OnCanPerformCheck (object sender, object args)
	{
		Stats s = GetComponentInParent<Stats>();
		if (s[type] < amount)
		{
			BaseAdjustment adj = (BaseAdjustment)args;
			adj.FlipToggle();
		}
	}

	void OnDidPerformNotification (object sender, object args)
	{
		Stats s = GetComponentInParent<Stats>();
		s[type] -= amount;
	}
	#endregion
}