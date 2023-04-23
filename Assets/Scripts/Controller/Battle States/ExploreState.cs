using UnityEngine;
using System.Collections;

public class ExploreState : BattleState 
{
	public override void Enter ()
	{
		base.Enter ();
		RefreshPrimaryStatPanel(pos);
		RefreshAwarenessLines(pos);
	}

	public override void Exit ()
	{
		base.Exit ();
		statPanelController.HidePrimary();
	}
	
	protected override void OnSubmit ()
	{
		owner.ChangeState<CommandSelectionState>();
	}
}