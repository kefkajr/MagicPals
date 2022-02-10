using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
		get { return _data[(int)s]; }
		set { SetValue(s, value, true); }
	}
	int[] _data = new int[ (int)StatTypes.Count ];
	#endregion
	
	#region Public
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
		
		_data[(int)type] = value;
		this.PostNotification(DidChangeNotification(type), oldValue);
	}
	#endregion
}