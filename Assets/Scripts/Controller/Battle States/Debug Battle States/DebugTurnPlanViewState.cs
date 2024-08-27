using System.Collections.Generic;
using UnityEngine;

public class DebugTurnPlanViewState : BattleState {

    List<TurnPlan> turnPlans;
    int currentIndex = 0;

    protected override void AddListeners() {
		InputController.moveEvent += OnMove;
        InputController.submitEvent += OnSubmit;
        InputController.cancelEvent += OnCancel;
	}
	
	protected override void RemoveListeners() {
		InputController.moveEvent -= OnMove;
		InputController.submitEvent -= OnSubmit;
		InputController.cancelEvent -= OnCancel;
	}

    public override void Enter() {
		base.Enter();
        Strategy strategy = owner.turn.actor.GetComponentInChildren<Strategy>();
		turnPlans = new TurnPlanFactory(owner.cpu).DebugTurnPlans(strategy);
        HighlighTurnPlan();
	}
	
	public override void Exit() {
		base.Exit();
        currentIndex = 0;
	}

    void OnMove(object sender, InfoEventArgs<Point> e) {
		if (e.info.x > 0 || e.info.y > 0) {
            currentIndex++;
        } else {
            currentIndex--;
        }
        HighlighTurnPlan();
	}

	protected override void OnSubmit() {
		owner.ChangeState<CommandSelectionState>();
	}

	protected override void OnCancel() {
		owner.ChangeState<CommandSelectionState>();
	}

    void HighlighTurnPlan() {
        if (currentIndex == turnPlans.Count) {
            owner.ChangeState<CommandSelectionState>();
            return;
        }
        TurnPlan turnPlan = turnPlans[currentIndex];
        turnPlan.CalculateScore(owner, owner.turn.actor);

        if (turnPlan.fireLocation != null) {
            AbilityArea area = turnPlan.ability.GetComponent<AbilityArea>();
            List<Tile> tiles = area.GetTilesInArea(owner.board, turnPlan.fireLocation.pos);
            owner.board.HighlightTiles(tiles, TileHighlightColorType.targetAreaHighlight);
            Console.Main.Log("Fire target: " + turnPlan.fireLocation);
        }
        owner.board.HighlightTiles(new List<Tile>{turnPlan.moveLocation}, TileHighlightColorType.moveRangeHighlight);
        Console.Main.Log("Move target: " + turnPlan.moveLocation);
        Console.Main.Log("Score: " + turnPlan.score);
        Debug.Log(turnPlan.description);
    }
    
}