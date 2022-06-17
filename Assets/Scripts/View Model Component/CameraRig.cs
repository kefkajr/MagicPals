using UnityEngine;
using System.Collections;

public class CameraRig : MonoBehaviour 
{
	public float speed = 3f;
	public Transform follow;


	public float rotateSpeed = 3f;
	public Directions currentDirection = Directions.North;
	TransformLocalEulerTweener rotateTweener;

	bool isRotating = false;

	void Awake ()
	{
		AddListeners();
	}
	
	void Update ()
	{
		if (follow)
			transform.position = Vector3.Lerp(transform.position, follow.position, speed * Time.deltaTime);
	}

	public void Exit()
	{
		RemoveListeners();
	}

	protected void OnDestroy()
	{
		RemoveListeners();
	}

	protected void AddListeners()
	{
		InputController.turnCameraEvent += TurnCamera;
	}

	protected void RemoveListeners()
	{
		InputController.turnCameraEvent -= TurnCamera;
	}

	protected void TurnCamera(object sender, InfoEventArgs<int> e)
	{
		int newDirectionValue = (int)currentDirection + e.info;

		if (newDirectionValue < 0)
			newDirectionValue = 3;
		if (newDirectionValue > 3)
			newDirectionValue = 0;

		currentDirection = (Directions)newDirectionValue;

		StartCoroutine(Turn());
	}

	protected virtual IEnumerator Turn()
	{
		if (isRotating) yield return null;

		isRotating = true;

		if (rotateTweener != null) rotateTweener.Stop();

		TransformLocalEulerTweener t = (TransformLocalEulerTweener)transform.RotateToLocal(currentDirection.ToEuler(), 0.25f, EasingEquations.EaseInOutQuad);

		// When rotating between North and West, we must make an exception so it looks like the unit
		// rotates the most efficient way (since 0 and 360 are treated the same)
		if (Mathf.Approximately(t.startTweenValue.y, 0f) && Mathf.Approximately(t.endTweenValue.y, 270f))
			t.startTweenValue = new Vector3(t.startTweenValue.x, 360f, t.startTweenValue.z);
		else if (Mathf.Approximately(t.startTweenValue.y, 270) && Mathf.Approximately(t.endTweenValue.y, 0))
			t.endTweenValue = new Vector3(t.startTweenValue.x, 360f, t.startTweenValue.z);

		rotateTweener = t;

		while (t != null)
			yield return null;

		isRotating = false;
	}

}