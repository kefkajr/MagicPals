using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Patrol {
    public List<PatrolNode> nodes;
    public Unit patroller { get; private set; }
    public int currentPatrolNodeIndex;
    private bool isReversed;

    void SetPatroller(Unit patroller) {
        this.patroller = patroller;
    }

    public void RemovePatroller(Unit patroller) {
        this.patroller = null;
    }

    public TurnPlan GetPlan(Unit unit, Board board) {
        SetPatroller(unit);
        
        PatrolNode node = nodes[currentPatrolNodeIndex];

        // If the unit is not already at the current position
        if (unit.tile.pos != node.targetMovePoint) {
            // Let the unit make its way to the current node
            // and also decide whether to reverse the patrol
            // based on the position they arrive from.
            // Not perfect, but it's something.
            int nextIndex = currentPatrolNodeIndex + 1;
            int prevIndex = currentPatrolNodeIndex - 1;
            if (nextIndex >= nodes.Count) {
                isReversed = true;
            } else if (prevIndex < 0) {
                isReversed = false;
            } else {
                Tile tile = board.GetTile(node.targetMovePoint);
                Tile nextTile = board.GetTile(nodes[nextIndex].targetMovePoint);
                List<Direction> nextDir = tile.GetDirections(nextTile);
                Tile prevTile = board.GetTile(nodes[prevIndex].targetMovePoint);
                List<Direction> prevDir = tile.GetDirections(prevTile);
                List<Direction> unitDir = unit.tile.GetDirections(tile);
                foreach (Direction dir in unitDir) {
                    if (nextDir.Contains(dir))
                        isReversed = false;
                    else if (prevDir.Contains(dir))
                        isReversed = true;
                }
            }
        } else { // If the unit is already at the current position
            // Move onto the next action
            int newIndex = isReversed ? currentPatrolNodeIndex - 1 : currentPatrolNodeIndex + 1;
            if (newIndex < 0 || newIndex >= nodes.Count) {
                isReversed = !isReversed;
                newIndex = isReversed ? currentPatrolNodeIndex - 1 : currentPatrolNodeIndex + 1;
            }
            currentPatrolNodeIndex = newIndex;
            node = nodes[currentPatrolNodeIndex];
        }
        
        TurnPlan plan = new();
        plan.moveLocation = board.GetTile(node.targetMovePoint) ?? plan.moveLocation;
        return plan;
    }

    public Direction GetCurrentDirection() {
        PatrolNode n = nodes[currentPatrolNodeIndex];
        Direction d = isReversed ? n.reverseDirection : n.facingDirection;
        return d;
    }
}

[System.Serializable]
public class PatrolNode {

    public Point targetMovePoint;
    public Direction facingDirection;
    public Direction reverseDirection;

    // Currently, the reverseDirecton is most useful in the middle of a patrol.
    // It's use at the ends are more particular.
    // - The first node will use the reverse direction.
    // - The last node will use the normal facing direction.
    // The exceptions are when the unit is entering the patrol either end.
    // - The first node will use the normal facing direction
    // - The last node will use the revere direction.
}