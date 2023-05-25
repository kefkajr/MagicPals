using UnityEngine;
using System.Collections;

public abstract class BaseVictoryCondition : MonoBehaviour
{
	#region Fields & Properties
	public Alliances Victor
	{
		get { return victor; } 
		protected set { victor = value; }
	}
	Alliances victor = Alliances.None;
	
	protected BattleController bc;
	#endregion
	
	#region MonoBehaviour
	protected virtual void Awake ()
	{
		bc = GetComponent<BattleController>();
	}
	
	protected virtual void OnEnable ()
	{
		this.AddObserver(OnHPDidChangeNotification, Stats.DidChangeNotification(StatTypes.HP));
	}
	
	protected virtual void OnDisable ()
	{
		this.RemoveObserver(OnHPDidChangeNotification, Stats.DidChangeNotification(StatTypes.HP));
	}
	#endregion
	
	#region Notification Handlers
	protected virtual void OnHPDidChangeNotification (object sender, object args)
	{
		CheckForGameOver();
	}
	#endregion
	
	#region Protected
	protected virtual void CheckForGameOver ()
	{
		if (PartyDefeated(Alliances.Hero))
			Victor = Alliances.Enemy;
	}
	
	protected virtual bool PartyDefeated (Alliances type)
	{
		for (int i = 0; i < bc.units.Count; ++i)
		{
			Unit unit = bc.units[i];
			Alliance a = unit.GetComponent<Alliance>();
			if (a == null)
				continue;
			
			if (a.type == type && !unit.IsDefeated())
				return false;
		}
		return true;
	}
	#endregion
}