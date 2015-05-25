using UnityEngine;
using System.Collections;

public class MoveTargetState : BattleState 
{
	protected override void OnMove (object sender, InfoEventArgs<Point> e)
	{
		SelectTile(e.info + pos);
	}

	protected override void OnFire (object sender, InfoEventArgs<int> e)
	{
		Debug.Log("Fire: " + e.info);
	}
}