using UnityEngine;
using System.Collections;

public static class FacingsExtensions
{
	// https://theliquidfire.com/2015/09/21/tactics-rpg-hit-rate/
	// If you were to take the dot product of the normalized vector
	// from our attacker to our defender, and the normalized vector
	// representing the angle the defender was facing,
	// then you would get numbers in the range of -1 (attacking from the front)
	// to +1 (attacking from the back).
	// These relationships are shown in the image below as the dog (attacker) approaches the cat (defender).
	public static Facings GetFacing (this Unit attacker, Unit target)
	{
		Vector2 targetDirection = target.dir.GetNormal();
		Vector2 approachDirection = ((Vector2)(target.tile.pos - attacker.tile.pos)).normalized;
		float dot = Vector2.Dot( approachDirection, targetDirection );
		if (dot >= 0.45f)
			return Facings.Back;
		if (dot <= -0.45f)
			return Facings.Front;
		return Facings.Side;
	}
}
