using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class BoardCreator : MonoBehaviour 
{
	#region Fields / Properties
	[SerializeField] public GameObject tileViewPrefab;
	[SerializeField] public GameObject wallViewPrefab;
	[SerializeField] public GameObject tileSelectionIndicatorPrefab;
	[SerializeField] public int width = 10;
	[SerializeField] public int depth = 10;
	[SerializeField] public int height = 8;
	[SerializeField] public Point pos;
	[SerializeField] public LevelData levelData;
	Dictionary<Point, Tile> tiles = new Dictionary<Point, Tile>();

	Transform marker
	{
		get
		{
			if (_marker == null)
			{
				GameObject instance = Instantiate(tileSelectionIndicatorPrefab);
				_marker = instance.transform;
			}
			return _marker;
		}
	}
	Transform _marker;
	#endregion

	#region Public
	public void Grow ()
	{
		GrowSingle(pos);
	}
	
	public void Shrink ()
	{
		ShrinkSingle(pos);
	}

	public void GrowArea ()
	{
		Rect r = RandomRect();
		GrowRect(r);
	}
	
	public void ShrinkArea ()
	{
		Rect r = RandomRect();
		ShrinkRect(r);
	}

	public void UpdateMarker ()
	{
		Tile t = tiles.ContainsKey(pos) ? tiles[pos] : null;
		marker.localPosition = t != null ? t.center : new Vector3(pos.x, 0, pos.y);
	}

	public void Clear ()
	{
		for (int i = transform.childCount - 1; i >= 0; --i)
			DestroyImmediate(transform.GetChild(i).gameObject);
		tiles.Clear();
	}

	[SerializeField] public Directions currentWallDirection = Directions.North;

	public void GrowWall()
	{
		GrowWall(pos, currentWallDirection);
	}

	public void ShrinkWall()
	{
		ShrinkWall(pos, currentWallDirection);
	}

	public void ThickenWall()
	{
		ThickenWall(pos, currentWallDirection);
	}

	public void ThinWall()
	{
		ThinWall(pos, currentWallDirection);
	}

	public void MoveWallIn()
	{
		MoveWall(pos, currentWallDirection, -1);
	}

	public void MoveWallOut()
	{
		MoveWall(pos, currentWallDirection, 1);
	}

	public void Save ()
	{
		string filePath = Application.dataPath + "/Resources/Levels";
		if (!Directory.Exists(filePath))
			CreateSaveDirectory ();
		
		LevelData board = ScriptableObject.CreateInstance<LevelData>();
		board.tiles = new List<TileData>( tiles.Count );
		foreach (Tile t in tiles.Values)
			board.tiles.Add( new TileData(t) );
		
		string fileName = string.Format("Assets/Resources/Levels/{1}.asset", filePath, levelData != null ? levelData.name : "New Level");
		AssetDatabase.CreateAsset(board, fileName);
	}

	public void Load ()
	{
		Clear();
		if (levelData == null)
			return;
		
		foreach (TileData td in levelData.tiles)
		{
			Tile t = CreateTile();
			t.Load(td);
			tiles.Add(t.pos, t);

			foreach (WallData wd in td.wallData)
			{
				Wall w = CreateWall();
				w.Load(t, wd);
				t.walls[wd.direction] = w;
			}
		}
	}
	#endregion

	#region Private
	Rect RandomRect ()
	{
		int x = Random.Range(0, width);
		int y = Random.Range(0, depth);
		int w = Random.Range(1, width - x + 1);
		int h = Random.Range(1, depth - y + 1);
		return new Rect(x, y, w, h);
	}

	void GrowRect (Rect rect)
	{
		for (int y = (int)rect.yMin; y < (int)rect.yMax; ++y)
		{
			for (int x = (int)rect.xMin; x < (int)rect.xMax; ++x)
			{
				Point p = new Point(x, y);
				GrowSingle(p);
			}
		}
	}
	
	void ShrinkRect (Rect rect)
	{
		for (int y = (int)rect.yMin; y < (int)rect.yMax; ++y)
		{
			for (int x = (int)rect.xMin; x < (int)rect.xMax; ++x)
			{
				Point p = new Point(x, y);
				ShrinkSingle(p);
			}
		}
	}

	Tile CreateTile ()
	{
		GameObject instance = Instantiate(tileViewPrefab);
		instance.transform.parent = transform;
		return instance.GetComponent<Tile>();
	}
	
	Tile GetOrCreateTile (Point p)
	{
		if (tiles.ContainsKey(p))
			return tiles[p];
		
		Tile t = CreateTile();
		t.Load(p, 0);
		tiles.Add(p, t);
		
		return t;
	}
	
	void GrowSingle (Point p)
	{
		Tile t = GetOrCreateTile(p);
		if (t.height < height)
			t.Grow();
	}

	void ShrinkSingle (Point p)
	{
		if (!tiles.ContainsKey(p))
			return;
		
		Tile t = tiles[p];
		t.Shrink();
		
		if (t.height <= 0)
		{
			tiles.Remove(p);
			foreach (Wall w in t.walls.Values)
				DestroyImmediate(w.gameObject);
			DestroyImmediate(t.gameObject);
		}
	}

	void GrowWall(Point p, Directions d)
	{
		Tile t = GetOrCreateTile(p);
		if (t.height < 1)
			GrowSingle(p);
		if (t.height < height)
		{
			Wall w = GetOrCreateWall(t, d);
			if (t.height + w.height < height)
				w.Grow();
		}
	}

	void ShrinkWall(Point p, Directions d)
	{
		Tile t = GetOrCreateTile(p);
		Wall w = GetOrCreateWall(t, d);

		w.Shrink();

		if (w.height <= 0)
		{
			t.walls.Remove(d);
			DestroyImmediate(w.gameObject);
		}
	}

	void ThickenWall(Point p, Directions d)
	{
		Tile t = GetOrCreateTile(p);
		if (t.height < 1)
			GrowSingle(p);
		Wall w = GetOrCreateWall(t, d);
		w.Thicken();
	}

	void ThinWall(Point p, Directions d)
	{
		Tile t = GetOrCreateTile(p);
		Wall w = GetOrCreateWall(t, d);

		w.Thin();

		if (w.thickness <= 0)
		{
			t.walls.Remove(d);
			DestroyImmediate(w.gameObject);
		}
	}

	void MoveWall(Point p, Directions d, int originChange)
	{
		Tile t = GetOrCreateTile(p);
		if (t.height < 1)
			GrowSingle(p);
		Wall w = GetOrCreateWall(t, d);
		w.MoveOrigin(originChange);
	}

	Wall CreateWall()
    {
		GameObject instance = Instantiate(wallViewPrefab);
		instance.transform.parent = transform;
		return instance.GetComponent<Wall>();
	}

	Wall GetOrCreateWall(Tile t, Directions d)
	{
		if (t.walls.ContainsKey(d))
			return t.walls[d];

		Wall w = CreateWall();

		w.Load(t, d);

		t.walls[d] = w;
		return w;
	}

	void CreateSaveDirectory ()
	{
		string filePath = Application.dataPath + "/Resources";
		if (!Directory.Exists(filePath))
			AssetDatabase.CreateFolder("Assets", "Resources");
		filePath += "/Levels";
		if (!Directory.Exists(filePath))
			AssetDatabase.CreateFolder("Assets/Resources", "Levels");
		AssetDatabase.Refresh();
	}
	#endregion
}
