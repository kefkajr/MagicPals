﻿using UnityEngine;
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
		owner.round = owner.gameObject.AddComponent<TurnOrderController>().Round();
		yield return null;
		//owner.ChangeState<CutSceneState>();
		owner.ChangeState<SelectUnitState>();
	}
	
	void SpawnTestUnits ()
	{
		string[] recipes = new string[]
		{
			"Alaois",
			"Enemy Rogue",
		};
		
		GameObject unitContainer = new GameObject("Units");
		unitContainer.transform.SetParent(owner.transform);
		
		List<Tile> locations = new List<Tile>(board.tiles.Values);
		for (int i = 0; i < recipes.Length; ++i)
		{
			int level = UnityEngine.Random.Range(9, 12);
			GameObject instance = UnitFactory.Create(recipes[i], level);
			instance.transform.SetParent(unitContainer.transform);
			
			int random = UnityEngine.Random.Range(0, locations.Count);
			Tile randomTile = locations[ random ];
			locations.RemoveAt(random);

			Unit unit = instance.GetComponent<Unit>();
			if (unit.name == "Alaois")
			{
				unit.Place(board.GetTile(new Point(1, 8)));
			}
			else if (unit.name == "Enemy Rogue")
			{
				unit.Place(board.GetTile(new Point(1, 6)));
			}
			unit.dir = (Directions)UnityEngine.Random.Range(0, 4);
			unit.Match();
			
			units.Add(unit);
		}
		
		SelectTile(units[0].tile.pos);

		// Allow all units to look at any other units
		foreach (Unit unit in units)
		{
			Perception perception = unit.GetComponent<Perception>();
			List <Awareness> awarenesses = perception.Look(board: board);
		}

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