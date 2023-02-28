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
		SpawnTestUnits();
		AddVictoryCondition();

		owner.awarenessController.InitializeAwarenessMap();

		owner.round = owner.gameObject.AddComponent<TurnOrderController>().Round();
		yield return null;
		//owner.ChangeState<CutSceneState>();
		owner.ChangeState<SelectUnitState>();
	}
	
	void SpawnTestUnits ()
	{
		string[] recipes = new string[]
		{
			"Cece",
			"Nessa",
			"Sentry",
			"Sentry"
		};
		
		GameObject unitContainer = new GameObject("Units");
		unitContainer.transform.SetParent(owner.transform);
		
		List<Tile> locations = new List<Tile>(board.tiles.Values);

		bool didPlaceFirstSentry = false;
		for (int i = 0; i < recipes.Length; ++i)
		{
			int level = UnityEngine.Random.Range(9, 12);
			GameObject instance = UnitFactory.Create(recipes[i], level);
			instance.transform.SetParent(unitContainer.transform);
			
			int random = UnityEngine.Random.Range(0, locations.Count);
			Tile randomTile = locations[ random ];
			locations.RemoveAt(random);

			Unit unit = instance.GetComponent<Unit>();
			if (unit.name == "Cece")
			{
				unit.Place(board.GetTile(new Point(8, 1)));
				unit.dir = Direction.South;
			} else if (unit.name == "Nessa")
			{
				unit.Place(board.GetTile(new Point(7, 1)));
				unit.dir = Direction.South;
			}
			else if (unit.name == "Sentry")
			{
				if (didPlaceFirstSentry)
				{
					unit.Place(board.GetTile(new Point(0, 9)));
					unit.name = "Wedge";
				}
				else
				{
					unit.Place(board.GetTile(new Point(1, 9)));
					unit.name = "Biggs";
					didPlaceFirstSentry = true;
				}
				unit.dir = Direction.West;
			}
			unit.Match();
			
			units.Add(unit);
		}
		
		SelectTile(units[0].tile.pos);
	}

	void AddVictoryCondition ()
	{
		DefeatTargetVictoryCondition vc = owner.gameObject.AddComponent<DefeatTargetVictoryCondition>();
		Unit enemy = units[ units.Count - 1 ];
		vc.target = enemy;
		Health health = enemy.GetComponent<Health>();
		health.MinHP = 10;
	}
}