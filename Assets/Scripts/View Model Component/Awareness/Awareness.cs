using System;

public class Awareness: IEquatable<Awareness>
{
	static int StartingAwarenessLevel = 10; // TODO: This should change with testing

	public Perception perception;
    public Stealth stealth;
	public Point pointOfInterest;
	public AwarenessType type;

	public int level = StartingAwarenessLevel;

	public Awareness(Perception perception, Stealth stealth, Point pointOfInterest, AwarenessType type)
    {
		this.perception = perception;
        this.stealth = stealth;
		this.pointOfInterest = pointOfInterest;
		this.type = type;
	}

	public bool Update(AwarenessType newType, Point newPointOfInterest)
    {
		switch(type)
        {
			case AwarenessType.Unaware:
				switch (newType)
				{
					case AwarenessType.MayHaveHeard:
					case AwarenessType.MayHaveSeen:
					case AwarenessType.Seen:
						pointOfInterest = newPointOfInterest;
						type = newType;
						level = StartingAwarenessLevel;
						return true;
					default: return false;
				}
			case AwarenessType.LostTrack:
				switch (newType)
				{
					case AwarenessType.Unaware:
						type = newType;
						return true;
					case AwarenessType.MayHaveHeard:
					case AwarenessType.MayHaveSeen:
					case AwarenessType.Seen:
						pointOfInterest = newPointOfInterest;
						type = newType;
						level = StartingAwarenessLevel;
						return true;
					default: return false;
				}
			case AwarenessType.MayHaveHeard:
				switch (newType)
				{
					case AwarenessType.LostTrack:
					case AwarenessType.Unaware:
						type = newType;
						return true;
					case AwarenessType.MayHaveHeard:
						pointOfInterest = newPointOfInterest;
						level = StartingAwarenessLevel;
						return false;
					case AwarenessType.MayHaveSeen:
					case AwarenessType.Seen:
						pointOfInterest = newPointOfInterest;
						type = newType;
						level = StartingAwarenessLevel;
						return true;
					default: return false;
				}
			case AwarenessType.MayHaveSeen:
				switch (newType)
				{
					case AwarenessType.Unaware:
						type = newType;
						return true;
					case AwarenessType.LostTrack:
						type = newType;
						level = StartingAwarenessLevel;
						return true;
					case AwarenessType.MayHaveSeen:
						pointOfInterest = newPointOfInterest;
						level = StartingAwarenessLevel;
						return false;
					case AwarenessType.Seen:
						pointOfInterest = newPointOfInterest;
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
						pointOfInterest = newPointOfInterest;
						level = StartingAwarenessLevel;
						return false;
					default: return false;
				}
			default: return false;
		}
    }

	public void Decay()
    {
		if (type == AwarenessType.Unaware)
			return; // Cannot decay further than Unaware

		level -= 1;
		if (level <= 0)
		{
			if (type == AwarenessType.Seen)
			{
				Update(AwarenessType.LostTrack, this.pointOfInterest);
			} else if (type != AwarenessType.Unaware)
            {
				Update(AwarenessType.Unaware, this.pointOfInterest);
			}
			Console.Main.Log(ToString());
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
		return stealth == a.stealth; //TODO Is this really appropriate?
	}

	public override int GetHashCode()
	{
		return stealth.GetInstanceID();
	}

	public override string ToString()
	{
		return string.Format("{0} > {1} > {2}", perception.unit.name, type.ActionVerb(), stealth.unit.name);
	}
    #endregion
}