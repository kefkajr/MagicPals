using UnityEngine;
using System;
using UnityEngine.InputSystem;

class Repeater
{
	const float threshold = 0.4f;
	const float rate = 0.25f;
	float _next;
	bool _hold;
	string _axis;
	
	public Repeater (string axisName)
	{
		_axis = axisName;
	}
	
	public int Update (int value)
	{
		int retValue = 0;		
		if (value != 0)
		{
			if (Time.time > _next)
			{
				retValue = value;
				_next = Time.time + (_hold ? rate : threshold);
				_hold = true;
			}
		}
		else
		{
			_hold = false;
			_next = 0;
		}
		
		return retValue;
	}
}

public class InputController : MonoBehaviour
{
	public static event EventHandler<InfoEventArgs<Point>> moveEvent;
	public static event Action cancelEvent;
	public static event Action submitEvent;
	public static event EventHandler<InfoEventArgs<int>> turnCameraEvent;
	public static event Action tiltCameraEvent;

	PlayerInputActions pia;

	Repeater _hor = new Repeater("Horizontal");
	Repeater _ver = new Repeater("Vertical");

	void Awake() {
		pia = new PlayerInputActions();
	}

	void OnEnable() {
		pia.Enable();

		pia.UI.Navigate.performed += DidNavigate;
		pia.UI.Submit.performed += DidSubmit;
		pia.UI.Cancel.performed += DidCancel;
		pia.UI.TurnCameraLeft.performed += delegate(InputAction.CallbackContext context) { DidCameraTurn(context, -1); };
		pia.UI.TurnCameraRight.performed += delegate(InputAction.CallbackContext context) { DidCameraTurn(context, 1); };
		pia.UI.TiltCamera.performed += DidCameraTilt;
	}

	void OnDisable() {
		pia.Disable();

		pia.UI.Navigate.performed -= DidNavigate;
		pia.UI.Submit.performed -= DidSubmit;
		pia.UI.Cancel.performed -= DidCancel;
		pia.UI.TurnCameraLeft.performed -= delegate(InputAction.CallbackContext context) { DidCameraTurn(context, -1); };
		pia.UI.TurnCameraRight.performed -= delegate(InputAction.CallbackContext context) { DidCameraTurn(context, 1); };
		pia.UI.TiltCamera.performed -= DidCameraTilt;
	}

	void DidNavigate(InputAction.CallbackContext context) {
		Vector2 v = context.ReadValue<Vector2>();
		int x = _hor.Update((int)v.x);
		int y = _ver.Update((int)v.y);
		if (x != 0 || y != 0)
		{
			if (moveEvent != null)
				moveEvent(this, new InfoEventArgs<Point>(new Point(x, y)));
		}
	}

	void DidSubmit(InputAction.CallbackContext context) {
		submitEvent();
	}

	void DidCancel(InputAction.CallbackContext context) {
		cancelEvent();
	}

	void DidCameraTurn(InputAction.CallbackContext context, int turn) {
		turnCameraEvent(this, new InfoEventArgs<int>(turn));
	}

	void DidCameraTilt(InputAction.CallbackContext context) {
		tiltCameraEvent();
	}
}
