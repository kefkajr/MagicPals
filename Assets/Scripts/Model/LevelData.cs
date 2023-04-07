using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelData : ScriptableObject 
{
	public List<TileData> tiles;
	public List<SpawnData> spawns;
	public List<Point> exits;
	public List<Patrol> patrols;
}