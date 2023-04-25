using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ExitMarker : MonoBehaviour
{
    const float stepHeight = 0.5f;
    public Point position;
    public int height;

	public ExitMarker(Point position)
	{
		this.position = position;
	}

    public void Match ()
	{
		transform.localPosition = new Vector3( position.x, height * stepHeight / 2f, position.y );
	}
}