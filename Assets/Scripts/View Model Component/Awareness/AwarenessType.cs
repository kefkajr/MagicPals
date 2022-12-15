using UnityEngine;

public enum AwarenessType
{
	Unaware, LostTrack, MayHaveHeard, MayHaveSeen, Seen
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
			case AwarenessType.LostTrack:
				return "lost track of";
			default: // Unaware:
				return "is unaware of";
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
			case AwarenessType.LostTrack:
				return Color.white;
			default: // Unaware:
				return Color.gray;
		};
	}
}