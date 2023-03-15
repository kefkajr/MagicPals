using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpawnMarker : MonoBehaviour
{
    const float stepHeight = 0.5f;

    public string recipeName;
    public TextMeshProUGUI recipeNameLabel;
    public Point position;
    public int height;
    public Transform directionMarker;
    public Direction direction;

	public SpawnMarker(string recipeName, Point position, Direction direction)
	{
		this.recipeName = recipeName;
		this.position = position;
        this.direction = direction;
	}

    public void Match ()
	{
		transform.localPosition = new Vector3( position.x, height * stepHeight / 2f, position.y );
		transform.localScale = new Vector3(1, 0.25f, 1);
        recipeNameLabel.text = recipeName;

        int directionValue = (int)direction;
        float rotationY = 0 + (directionValue * 90);
        directionMarker.localEulerAngles = direction.ToEuler();
	}
}

[System.Serializable]
public class SpawnData
{
    public string recipeName;
    public Point position;
    public Direction direction;

	public SpawnData(string recipeName, Point position, Direction direction)
	{
		this.recipeName = recipeName;
		this.position = position;
        this.direction = direction;
	}
}
