using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyBox;


[System.Serializable]
public class Patrol
{
    public List<PatrolAction> patrolActions;
    public Unit patroller { get; private set; }
    public int currentPatrolActionIndex;
    private bool isReversed;

    void SetPatroller(Unit patroller) {
        this.patroller = patroller;
    }

    public void RemovePatroller(Unit patroller) {
        this.patroller = null;
    }

    public void SetPlan(PlanOfAttack poa, Unit unit, Board board) {
        SetPatroller(unit);
        PatrolAction patrolAction = patrolActions[currentPatrolActionIndex];

        // If the unit is not already at the current position
        if (unit.tile.pos != patrolAction.targetMovePoint) {

        } else { // If the unit is already the current position
            // Move onto the next action
            int newIndex = isReversed ? currentPatrolActionIndex - 1 : currentPatrolActionIndex + 1;
            if (newIndex < 0 || newIndex >= patrolActions.Count) {
                isReversed = !isReversed;
                newIndex = isReversed ? currentPatrolActionIndex - 1 : currentPatrolActionIndex + 1;
            }
            currentPatrolActionIndex = newIndex;
            patrolAction = patrolActions[currentPatrolActionIndex];
        }

        switch (patrolAction.type) {
            case PatrolActionType.Move:
                poa.moveLocation = board.GetTile(patrolAction.targetMovePoint) ?? poa.moveLocation;
                break;
            case PatrolActionType.Face:
                poa.moveLocation = board.GetTile(patrolAction.targetMovePoint) ?? poa.moveLocation;
                poa.attackDirection = patrolAction.facingDirection;
                break;
        }
    }
}

public enum PatrolActionType {
    Move, Face
}

[System.Serializable]
public class PatrolAction {

    public PatrolActionType type;
    public Point targetMovePoint;
    [ConditionalField(nameof(type), false, PatrolActionType.Face)] public Direction facingDirection;
}