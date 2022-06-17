using UnityEngine;
using System.Collections;

public class ExploreState : BattleState 
{
	public override void Enter ()
	{
		base.Enter ();
		RefreshPrimaryStatPanel(pos);
	}

	public override void Exit ()
	{
		base.Exit ();
		statPanelController.HidePrimary();
	}

	protected override void OnMove(object sender, MoveEventData d)
	{
		SelectTile(d.pointTranslatedByCameraDirection + pos);
		RefreshPrimaryStatPanel(pos);
	}
	
	protected override void OnFire (object sender, InfoEventArgs<int> e)
	{
		if (e.info == 0)
			owner.ChangeState<CommandSelectionState>();
	}
}