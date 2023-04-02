using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Stats : MonoBehaviour
{
	#region Notifications
	public static string WillChangeNotification (StatTypes type)
	{
		if (!_willChangeNotifications.ContainsKey(type))
			_willChangeNotifications.Add(type, string.Format("Stats.{0}WillChange", type.ToString()));
		return _willChangeNotifications[type];
	}
	
	public static string DidChangeNotification (StatTypes type)
	{
		if (!_didChangeNotifications.ContainsKey(type))
			_didChangeNotifications.Add(type, string.Format("Stats.{0}DidChange", type.ToString()));
		return _didChangeNotifications[type];
	}
	
	static Dictionary<StatTypes, string> _willChangeNotifications = new Dictionary<StatTypes, string>();
	static Dictionary<StatTypes, string> _didChangeNotifications = new Dictionary<StatTypes, string>();
	#endregion
	
	#region Fields / Properties
	public int this[StatTypes s]
	{
		get { return data[(int)s].value; }
		set { SetValue(s, value, true); }
	}
	public Stat[] data = Stats.AllStatTypes().Select(t => new Stat(t)).ToArray();
	#endregion
	
	#region Public
    void Awake() {
		foreach(StatTypes type in AllStatTypes()) {
			Stat stat = new Stat(type);
		}
	}

	public void SetValue (StatTypes type, int value, bool allowAdjustments)
	{
		int oldValue = this[type];
		if (oldValue == value)
			return;
		
		if (allowAdjustments)
		{
			// Allow adjustments to the rule here
			ValueChangeAdjustment adj = new ValueChangeAdjustment( oldValue, value );
			
			// The notification is unique per stat type
			this.PostNotification(WillChangeNotification(type), adj);
			
			// Did anything modify the value?
			value = Mathf.FloorToInt(adj.GetModifiedValue());
			
			// Did something nullify the change?
			if (adj.toggle == false || value == oldValue)
				return;
		}
		
		data[(int)type].value = value;
		this.PostNotification(DidChangeNotification(type), oldValue);
	}
	#endregion

	static StatTypes[] AllStatTypes() {
		return new StatTypes[] { StatTypes.LVL, StatTypes.EXP, StatTypes.HP, StatTypes.MHP, StatTypes.MP, StatTypes.MMP, StatTypes.ATK, StatTypes.DEF, StatTypes.MAT, StatTypes.MDF, StatTypes.EVD, StatTypes.RES, StatTypes.SPD, StatTypes.MOV, StatTypes.JMP, StatTypes.CTR };
	}
}