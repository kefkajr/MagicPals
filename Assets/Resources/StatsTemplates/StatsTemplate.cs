using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "Create new stats template")]
public class StatsTemplate : ScriptableObject 
{
	public SerializableDictionary<StatTypes, Stat> data = new SerializableDictionary<StatTypes, Stat>(Stats.TemplateStatTypes().ToDictionary(t => t, t => new Stat(t)));
}