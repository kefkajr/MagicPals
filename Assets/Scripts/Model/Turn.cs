using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Turn 
{
	public Unit actor;
	public bool lockMove;
	public Ability ability;
	public Tile abilityEpicenterTile;
	public List<Tile> targets;
	public TurnPlan plan;
	Tile startTile;
	Direction startDir;

	List<ActionType> actionTypesTaken;
	List<TileAndDirectionPair> moveHistory;
	public bool hasUnitMoved { get { return actionTypesTaken.Contains(ActionType.Move); } }
	public bool hasUnitActed { get { return actionTypesTaken.Contains(ActionType.Major); } }

	public void Change (Unit current)
	{
		actor = current;
		lockMove = false;
		startTile = actor.tile;
		startDir = actor.dir;
		plan = null;
		actionTypesTaken = new List<ActionType>();
		moveHistory = new List<TileAndDirectionPair>{ new TileAndDirectionPair(actor.tile, actor.dir) };
	}

	public void TakeActionType(ActionType actionType) {
		actionTypesTaken.Add(actionType);

		switch(actionType) {
			case ActionType.Move:
				moveHistory.Add(new TileAndDirectionPair(actor.tile, actor.dir));
				break;
			default: break;
		}
	}

	public void RemoveActionType(ActionType actionType) {
		actionTypesTaken.Remove(actionType);

		switch(actionType) {
			case ActionType.Move:
				if (moveHistory.Count > 0) {
					moveHistory.RemoveAt(moveHistory.Count - 1);
					actor.Place(moveHistory[moveHistory.Count - 1].tile);
					actor.dir = moveHistory[moveHistory.Count - 1].direction;
				}
				actor.Match();
				break;
			default: break;
		}
	}

	struct TileAndDirectionPair {
		public Tile tile;
		public Direction direction;
		public TileAndDirectionPair(Tile tile, Direction direction) { this.tile = tile; this.direction = direction; }
	}
}