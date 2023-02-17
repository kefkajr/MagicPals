using UnityEngine;
using System.Collections;

[System.Serializable]
public class Alliance : MonoBehaviour
{
	public Alliances type;
	public bool confused;

	public bool IsMatch (Alliance other, TargetType targets)
	{
		bool isMatch = false;
		switch (targets)
		{
		case TargetType.Self:
			isMatch = other == this;
			break;
		case TargetType.Ally:
			isMatch = type == other.type;
			break;
		case TargetType.Foe:
			isMatch = (type != other.type) && other.type != Alliances.Neutral;
			break;
		}
		return confused ? !isMatch : isMatch;
	}
}