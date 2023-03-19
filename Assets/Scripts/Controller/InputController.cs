﻿using UnityEngine;
using System;
using UnityEngine.InputSystem;

class Repeater
{
	const float threshold = 0.4f;
	const float rate = 0.15f;
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
	public static event Action submitEvent;
	public static event Action cancelEvent;
	public static event EventHandler<float> turnCameraEvent;
	public static event Action tiltCameraEvent;

	PlayerInput pia;

	Repeater _hor = new Repeater("Horizontal");
	Repeater _ver = new Repeater("Vertical");

	void Awake() {
		pia = GetComponent<PlayerInput>();
	}

	void OnEnable() {
		pia.actions["Submit"].performed += DidSubmit;
		pia.actions["Cancel"].performed += DidCancel;
		pia.actions["Turn Camera"].performed += DidCameraTurn;
		pia.actions["Tilt Camera"].performed += DidCameraTilt;
	}

	void OnDisable() {
		pia.actions["Submit"].performed -= DidSubmit;
		pia.actions["Cancel"].performed -= DidCancel;
		pia.actions["Turn Camera"].performed -= DidCameraTurn;
		pia.actions["Tilt Camera"].performed -= DidCameraTilt;
	}

	void Update() {
		Vector2 v = pia.actions["Navigate"].ReadValue<Vector2>();
		if (v != null)
			DidNavigate(v);
	}

	void DidNavigate(Vector2 v) {
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

	void DidCameraTurn(InputAction.CallbackContext context) {
		turnCameraEvent(this, context.ReadValue<float>());
	}

	void DidCameraTilt(InputAction.CallbackContext context) {
		tiltCameraEvent();
	}
}
