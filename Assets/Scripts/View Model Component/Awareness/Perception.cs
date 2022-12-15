using UnityEngine;
using System.Collections.Generic;

public class Perception : MonoBehaviour
{
	public Vector2 viewingRange = Vector2.zero; // cone, length and diameter
	public float hearingRange = 0f; // radius of a circle
	public HashSet<Awareness> awarenesses = new HashSet<Awareness>(); // list of awarenesses, known stealths and their types

	public Unit unit { get { return GetComponentInParent<Unit>(); } }
}