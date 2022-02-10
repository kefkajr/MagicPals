using UnityEngine;
using System.Collections;

public class BaseAdjustment 
{
	public bool toggle { get; private set; }
	public readonly bool defaultToggle;
	
	public BaseAdjustment (bool defaultToggle)
	{
		this.defaultToggle = defaultToggle;
		toggle = defaultToggle;
	}
	
	public void FlipToggle ()
	{
		toggle = !defaultToggle;
	}
}