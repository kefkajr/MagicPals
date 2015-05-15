using UnityEngine;
using System.Collections;

public class Demo : MonoBehaviour 
{
	void OnEnable ()
	{
		InputController.moveEvent += OnMoveEvent;
		InputController.fireEvent += OnFireEvent;
	}
	
	void OnDisable ()
	{
		InputController.moveEvent -= OnMoveEvent;
		InputController.fireEvent -= OnFireEvent;
	}

	void OnMoveEvent (object sender, InfoEventArgs<Point> e)
	{
		Debug.Log("Move " + e.info.ToString());
	}

	void OnFireEvent (object sender, InfoEventArgs<int> e)
	{
		Debug.Log("Fire " + e.info);
	}
}
