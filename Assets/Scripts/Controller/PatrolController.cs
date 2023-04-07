using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class PatrolController : MonoBehaviour
{
    protected BattleController battleController;
    protected Board board { get { return battleController.board; } }
    public List<Patrol> patrols = new List<Patrol>();

    protected virtual void Awake()
    {
        battleController = GetComponent<BattleController>();
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

            int patrolsActionsToCheck = patrol.patrolActions.Count;
            while (patrolsActionsToCheck > 0) {
                for (int i = 0; i < patrol.patrolActions.Count; ++i) {
                    PatrolAction patrolAction = patrol.patrolActions[i];
                    yield return board.FindPath(unit, unit.tile, board.GetTile(patrolAction.targetMovePoint), delegate (List<Tile> finalPath) {
                        int distance = finalPath.Count;
                        if (distance < shortestDistance) {
                            shortestDistance = distance;
                            nearestPatrol = patrol;
                            nearestPatrol.currentPatrolActionIndex = i;
                        }
                        --patrolsActionsToCheck;
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
}