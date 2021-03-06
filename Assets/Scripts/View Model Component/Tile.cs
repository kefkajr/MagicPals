using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tile : MonoBehaviour 
{
	#region Const
	public const float stepHeight = 0.25f;
	#endregion

	#region Fields / Properties
	public Point pos;
	public int height;
	public Vector3 center { get { return new Vector3(pos.x, height * stepHeight, pos.y); }}
	public Dictionary<Directions, Wall> walls = new Dictionary<Directions, Wall>();
	public GameObject occupant;
	public Trap trap;
	public List<Merchandise> items;

	[HideInInspector] public Tile prev;
	[HideInInspector] public int distance;
	#endregion

	#region Public
	public void Grow ()
	{
		height++;
		Match();
	}
	
	public void Shrink ()
	{
		height--;
		Match ();
	}

	public void Load (Point p, int h)
	{
		pos = p;
		height = h;
		Match();
	}
	
	public void Load (TileData t)
	{
		Load (t.point, t.height);
	}

	void Match ()
	{
		transform.localPosition = new Vector3( pos.x, height * stepHeight / 2f, pos.y );
		transform.localScale = new Vector3(1, height * stepHeight, 1);

		foreach (Wall w in walls.Values)
        {
			w.Load(this, w.direction);
        }
	}

	public static bool DoesWallSeparateTiles(Tile tile1, Tile tile2)
    {
		Directions dir1 = tile1.GetDirection(tile2);
		Directions dir2 = tile2.GetDirection(tile1);
		return tile1.walls.ContainsKey(dir1) || tile2.walls.ContainsKey(dir2);
	}
	#endregion

	void OnDrawGizmos()
	{
		GUI.color = Color.black;
		UnityEditor.Handles.Label(transform.position, string.Format("{0}, {1}", pos.x, pos.y));
	}
}

[System.Serializable]
public class TileData
{
	public Point point;
	public int height;
	public List<WallData> wallData = new List<WallData>();

	public TileData(Tile tile)
	{
		this.point = tile.pos;
		this.height = tile.height;
		foreach (Wall wall in tile.walls.Values)
		{
			this.wallData.Add(new WallData(wall));
		}
	}
}