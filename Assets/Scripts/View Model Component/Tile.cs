using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tile : MonoBehaviour 
{
	#region Const
	public const float stepHeight = 0.25f;
	#endregion

	#region Fields / Properties
	[SerializeField] public MeshRenderer highlightMeshRenderer;
	public Point pos;
	public int height;
	public Vector3 center { get { return new Vector3(pos.x, height * stepHeight, pos.y); }}
	public Dictionary<Direction, Wall> walls = new Dictionary<Direction, Wall>();
	public GameObject occupant;
	public Trap trap;
	public List<Merchandise> items;

	[HideInInspector] public Tile prev;
	[HideInInspector] public int distance;

	[HideInInspector] public int g;
	[HideInInspector] public int h;

	[HideInInspector] public int f { get { return g + h; } }
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

	public void SetHighlightColor(Color color) {
		highlightMeshRenderer.gameObject.SetActive(true);
		highlightMeshRenderer.material.SetColor("_Color", color);
	}

	public void HideHighlightColor() {
		highlightMeshRenderer.gameObject.SetActive(false);
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

	#endregion

	public bool isBeingPerceived;
	public float gizmoAlpha;
	public Color gizmoColor = Color.red;

	void OnDrawGizmos()
	{
		GUI.color = Color.black;
		UnityEditor.Handles.Label(transform.position, string.Format("{0}, {1}", pos.x, pos.y));

		if (isBeingPerceived) 
		{
			Color color = gizmoColor;
			color.a = gizmoAlpha;
			Gizmos.color = color;
			Gizmos.DrawSphere(center, 0.2f);
			gizmoAlpha -= 0.01f;

			if (gizmoAlpha <= 0)
				isBeingPerceived = false;
		}
	}

	public override string ToString()
	{
		return string.Format("Tile: {0}", pos.ToString());
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