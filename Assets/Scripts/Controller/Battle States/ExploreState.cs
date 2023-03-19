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
	
	protected override void OnSubmit ()
	{
		owner.ChangeState<CommandSelectionState>();
	}
}