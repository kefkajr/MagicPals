using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class PatrolController : MonoBehaviour {
    protected BattleController battleController;
    protected Board board { get { return battleController.board; } }
    public List<Patrol> patrols = new List<Patrol>();

    protected virtual void Awake() {
        battleController = GetComponentInParent<BattleController>();
    }

    public void Intialize(List<Patrol> patrols) {
        this.patrols = patrols;
    }

    public IEnumerator GetNearestAvailablePatrol(Unit unit, Action<Patrol> completionHandler) {
		int shortestDistance = int.MaxValue;
		Patrol nearestPatrol = null;
		foreach (Patrol patrol in patrols) {
			if (patrol.patroller != null)
				continue;

            int patrolNodesToCheck = patrol.nodes.Count;
            while (patrolNodesToCheck > 0) {
                for (int i = 0; i < patrol.nodes.Count; ++i) {
                    PatrolNode patrolNode = patrol.nodes[i];
                    yield return board.FindPath(unit, unit.tile, board.GetTile(patrolNode.targetMovePoint), delegate (List<Tile> finalPath) {
                        int distance = finalPath.Count;
                        if (distance < shortestDistance) {
                            shortestDistance = distance;
                            nearestPatrol = patrol;
                            nearestPatrol.currentPatrolNodeIndex = i;
                        }
                        --patrolNodesToCheck;
                    });				
                }
            }
		}
		completionHandler(nearestPatrol);
	}

    public Patrol GetPatrolForUnit(Unit unit) {
        Patrol patrol = patrols.Find(p => p.patroller == unit);
        return patrol;
    }

    public void RemoveUnitFromPatrol(Unit unit) {
        Patrol patrol = GetPatrolForUnit(unit);
        if (patrol != null)
            patrol.RemovePatroller(unit);
    }
}