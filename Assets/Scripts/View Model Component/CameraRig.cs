using UnityEngine;
using System.Collections;

public class CameraRig : MonoBehaviour 
{
	public float speed = 3f;
	public Transform follow;


	public float rotateDuration = 0.25f;
	public Directions currentDirection = Directions.North;
	TransformLocalEulerTweener rotateTweener;
	bool isRotating = false;

	public Transform pitchTransform;
	public float tiltDuration = 0.25f;
	TransformLocalEulerTweener tiltTweener;
	bool isTilting = false;
	bool isTilted = false;

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
		InputController.tiltCameraEvent += TiltCamera;
	}

	protected void RemoveListeners()
	{
		InputController.turnCameraEvent -= TurnCamera;
	}

	protected void TurnCamera(object sender, InfoEventArgs<int> e)
	{
		if (isRotating) return;

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
		isRotating = true;

		if (rotateTweener != null) rotateTweener.Stop();

		TransformLocalEulerTweener t = (TransformLocalEulerTweener)transform.RotateToLocal(currentDirection.ToEuler(), rotateDuration, EasingEquations.EaseInOutQuad);

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

	protected void TiltCamera(object sender, int i)
	{
		if (isTilting) return;

		isTilted = !isTilted;

		StartCoroutine(Tilt());
	}

	protected virtual IEnumerator Tilt()
	{
		isTilting = true;

		if (tiltTweener != null) tiltTweener.Stop();

		Vector3 euler = new Vector3(isTilted ? 0 : 36, 0, 0);

		TransformLocalEulerTweener t = (TransformLocalEulerTweener)pitchTransform.RotateToLocal(euler, tiltDuration, EasingEquations.EaseInOutQuad);

		tiltTweener = t;

		while (t != null)
			yield return null;

		isTilting = false;
	}

}