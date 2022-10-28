using UnityEngine;
using System.Collections;

public class Driver : MonoBehaviour 
{
	public DriverType normal;
	public DriverType special;

	public DriverType Current
	{
		get
		{
			return special != DriverType.None ? special : normal;
		}
	}
}