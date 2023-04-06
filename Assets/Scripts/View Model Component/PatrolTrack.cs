using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyBox;

public class Patrol
{
    public PatrolTrack track;
    public Unit patroller;

    public Patrol (PatrolTrack track) {
        this.track = track;
    }
}

[System.Serializable]
public class PatrolTrack
{
    public List<PatrolAction> patrolActions;
}

public enum PatrolActionType {
    Move, Face
}

[System.Serializable]
public class PatrolAction {

    public PatrolActionType type;
    public Point targetMovePoint;
    [ConditionalField(nameof(type), false, PatrolActionType.Face)] public Direction facingDirection;
    protected void Patrol(PlanOfAttack poa, Board board) {
        switch (type) {
            case PatrolActionType.Move:
                poa.moveLocation = board.GetTile(targetMovePoint) ?? poa.moveLocation;
                break;
            case PatrolActionType.Face:
                poa.attackDirection = facingDirection;
                break;
        }
    }
}