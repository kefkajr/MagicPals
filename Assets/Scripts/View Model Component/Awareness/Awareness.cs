using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Awareness: IEquatable<Awareness>
{
	static int StartingAwarenessLevel = 5; // TODO: This should change with testing

	public Perception perception;
    public Stealth stealth;
    public AwarenessType type;
	public int level = StartingAwarenessLevel;

	public Unit unit { get { return perception.GetComponentInParent<Unit>(); } }
	public bool isExpired {  get { return type != AwarenessType.Seen && level <= 0; } }

	public Awareness(Perception perception, Stealth stealth, AwarenessType type)
    {
		this.perception = perception;
        this.stealth = stealth;
        this.type = type;
	}

	public bool Update(AwarenessType newType)
    {
		switch(type)
        {
			case AwarenessType.LostTrack:
				switch (newType)
				{
					case AwarenessType.MayHaveHeard:
					case AwarenessType.MayHaveSeen:
					case AwarenessType.Seen:
						type = newType;
						level = StartingAwarenessLevel;
						return true;
					default: return false;
				}
			case AwarenessType.MayHaveHeard:
				switch (newType)
				{
					case AwarenessType.LostTrack:
					case AwarenessType.MayHaveSeen:
					case AwarenessType.Seen:
						type = newType;
						level = StartingAwarenessLevel;
						return true;
					default: return false;
				}
			case AwarenessType.MayHaveSeen:
				switch (newType)
				{
					case AwarenessType.LostTrack:
					case AwarenessType.Seen:
						type = newType;
						level = StartingAwarenessLevel;
						return true;
					default: return false;
				}
			case AwarenessType.Seen:
				switch (newType)
				{
					case AwarenessType.LostTrack:
						type = newType;
						level = StartingAwarenessLevel;
						return true;
					case AwarenessType.Seen:
						level = StartingAwarenessLevel;
						return false;
					default: return false;
				}
			default: return false;
		}
    }

	public void Decay()
    {
		level -= 1;
		if (level <= 0 && type == AwarenessType.Seen)
		{
			Update(AwarenessType.LostTrack);
			Debug.Log(ToString());
		}
	}

    #region Equatable & Other
    public override bool Equals(object obj)
	{
		if (obj is Awareness)
		{
			Awareness a = (Awareness)obj;
			return Equals(a);
		}
		return false;
	}

	public bool Equals(Awareness a)
	{
		return stealth == a.stealth;
	}

	public override int GetHashCode()
	{
		return stealth.GetInstanceID();
	}

	public override string ToString()
	{
		return string.Format("{0} > {1} > {2}", unit.name, type.ActionVerb(), stealth.unit.name);
	}
    #endregion
}

public enum AwarenessType
{
	LostTrack, MayHaveHeard, MayHaveSeen, Seen
}

public static class AwarenessTypeExtensions
{
	public static string ActionVerb(this AwarenessType type)
	{
		switch (type)
		{
			case AwarenessType.MayHaveHeard:
				return "may have heard";
			case AwarenessType.MayHaveSeen:
				return "may have seen";
			case AwarenessType.Seen:
				return "definitely saw";
			default: // LostTrack:
				return "lost track of";
		};
	}

	public static Color GizmoColor(this AwarenessType type)
	{
		switch (type)
		{
			case AwarenessType.MayHaveHeard:
				return Color.cyan;
			case AwarenessType.MayHaveSeen:
				return Color.yellow;
			case AwarenessType.Seen:
				return Color.red;
			default: // LostTrack:
				return Color.white;
		};
	}
}