using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Create new awareness recipe")]
public class AwarenessRecipe : ScriptableObject
{
	public Vector2 viewingRange; // cone, length and diameter
	public float hearingRange; // radius of a circle
}
