using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(BoardCreator))]
public class BoardCreatorInspector : Editor 
{
	public BoardCreator current
	{
		get
		{
			return (BoardCreator)target;
		}
	}

	public override void OnInspectorGUI ()
	{
		DrawDefaultInspector();

		GUILayout.Label("Marker Position");
		if (GUILayout.Button("North"))
			current.MoveMarker(Directions.North);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("West"))
			current.MoveMarker(Directions.West);
		if (GUILayout.Button("East"))
			current.MoveMarker(Directions.East);
		GUILayout.EndHorizontal();
		if (GUILayout.Button("South"))
			current.MoveMarker(Directions.South);

		GUILayout.Label("Tile");
		if (GUILayout.Button("Grow"))
			current.Grow();
		if (GUILayout.Button("Shrink"))
			current.Shrink();
		if (GUILayout.Button("Grow Area"))
			current.GrowArea();
		if (GUILayout.Button("Shrink Area"))
			current.ShrinkArea();

		GUILayout.Label("Wall Direction");
		if (GUILayout.Button("North"))
			current.ChangeWallDirection(Directions.North);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("West"))
			current.ChangeWallDirection(Directions.West);
		if (GUILayout.Button("East"))
			current.ChangeWallDirection(Directions.East);
		GUILayout.EndHorizontal();
		if (GUILayout.Button("South"))
			current.ChangeWallDirection(Directions.South);

		GUILayout.Label("Wall");
		if (GUILayout.Button("Grow Wall"))
			current.GrowWall();
		if (GUILayout.Button("Shrink Wall"))
			current.ShrinkWall();
		if (GUILayout.Button("Thicken Wall"))
			current.ThickenWall();
		if (GUILayout.Button("Thin Wall"))
			current.ThinWall();
		if (GUILayout.Button("Move Wall In"))
			current.MoveWallIn();
		if (GUILayout.Button("Move Wall Out"))
			current.MoveWallOut();

		GUILayout.Label("Data");
		if (GUILayout.Button("Save"))
			current.Save();
		if (GUILayout.Button("Load"))
			current.Load();
		if(GUILayout.Button("Clear"))
			current.Clear();

		if (GUI.changed)
			current.UpdateMarker ();
	}
}
