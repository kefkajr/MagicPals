using UnityEngine;
using System.Collections;

public class ExploreState : BattleState 
{
	public override void Enter ()
	{
		base.Enter ();
		board.DeHighlightAllTiles();
		RefreshPrimaryStatPanel(pos);
		DisplayAwarenessInfoForUnitAtPoint(pos);
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