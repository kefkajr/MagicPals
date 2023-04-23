using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InitBattleState : BattleState 
{
	public override void Enter ()
	{
		base.Enter ();
		StartCoroutine(Init());
	}
	
	IEnumerator Init ()
	{
		board.Load( levelData );
		Point p = new Point((int)levelData.tiles[0].point.x, (int)levelData.tiles[0].point.y);
		SelectTile(p);
		SpawnUnits();
		AddVictoryCondition();

		owner.awarenessController.Setup(owner);
		owner.patrolController.Intialize(levelData.patrols);
		owner.uiController.Setup(units.Count);

		owner.round = owner.gameObject.AddComponent<TurnOrderController>().Round();
		yield return null;
		//owner.ChangeState<CutSceneState>();
		owner.ChangeState<SelectUnitState>();
	}
	
	void SpawnUnits ()
	{	
		GameObject unitContainer = new GameObject("Units");
		unitContainer.transform.SetParent(owner.transform);

		GameObject escapedUnitContainer = new GameObject("Escaped Units");
		escapedUnitContainer.transform.SetParent(owner.transform);
		
		List<Tile> locations = new List<Tile>(board.tiles.Values);

		bool didPlaceFirstSentry = false;
		for (int i = 0; i < levelData.spawns.Count; ++i)
		{
			SpawnData spawn = levelData.spawns[i];
			int level = UnityEngine.Random.Range(9, 12);
			GameObject instance = UnitFactory.Create(spawn.recipeName);
			instance.transform.SetParent(unitContainer.transform);
			
			int random = UnityEngine.Random.Range(0, locations.Count);
			Tile randomTile = locations[ random ];
			locations.RemoveAt(random);

			Unit unit = instance.GetComponent<Unit>();
			unit.Place(board.GetTile(spawn.position));
			unit.dir = spawn.direction;
			unit.Match();

			if (unit.name == "Sentry") {
				if (didPlaceFirstSentry) {
					unit.name = "Wedge";
				} else {
					unit.name = "Biggs";
					didPlaceFirstSentry = true;
				}
			}
			
			units.Add(unit);
		}
		
		SelectTile(units[0].tile.pos);
	}

	void AddVictoryCondition ()
	{

		// Old victory condition
		// DefeatTargetVictoryCondition vc = owner.gameObject.AddComponent<DefeatTargetVictoryCondition>();
		// Unit enemy = units[ units.Count - 1 ];
		// vc.target = enemy;
		// Health health = enemy.GetComponent<Health>();
		// health.MinHP = 10;
		EscapeVictoryCondition vc = owner.gameObject.AddComponent<EscapeVictoryCondition>();
	}
}