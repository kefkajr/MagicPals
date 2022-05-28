using UnityEngine;
using System.Collections;

public class CameraRig : MonoBehaviour 
{
	public float speed = 3f;
	public Transform follow;


	public float rotateSpeed = 3f;
	public Transform pitch;
	Vector3 targetAngles;

	bool isRotating = false;

	void Awake ()
	{
		AddListeners();
		targetAngles = pitch.transform.localEulerAngles;
	}
	
	void Update ()
	{
		if (follow)
			transform.position = Vector3.Lerp(transform.position, follow.position, speed * Time.deltaTime);

		//if (pitch.transform.eulerAngles.y != targetAngles.y)
		//{
		//	Debug.Log(pitch.transform.eulerAngles.y.ToString() + " " + targetAngles.y.ToString());
		//	pitch.transform.eulerAngles = Vector3.Lerp(pitch.transform.eulerAngles, targetAngles, rotateSpeed * Time.deltaTime);
		//}
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
		if (e.info == -1)
			targetAngles.y -= 90;
		else
			targetAngles.y += 90;

		if (targetAngles.y > 360)
			targetAngles.y -= 360;
		else if (targetAngles.y < 0)
			targetAngles.y += 360;

		StartCoroutine(Rotate());
	}

	// Might be a better way to do this (Rotate instead of RotateToLocal perhaps?), but this works for now!
	private IEnumerator Rotate()
	{
		TransformLocalEulerTweener t = (TransformLocalEulerTweener)pitch.transform.RotateToLocal(targetAngles, 0.25f, EasingEquations.EaseInOutQuad);

		if (Mathf.Approximately(t.startTweenValue.y, 0f) && Mathf.Approximately(t.endTweenValue.y, 270f))
			t.startTweenValue = new Vector3(t.startTweenValue.x, 360f, t.startTweenValue.z);
		else if (Mathf.Approximately(t.startTweenValue.y, 270) && Mathf.Approximately(t.endTweenValue.y, 0))
			t.endTweenValue = new Vector3(t.startTweenValue.x, 360f, t.startTweenValue.z);
		while (t != null)
			yield return null;
	}

}