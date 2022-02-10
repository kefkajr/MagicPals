using UnityEngine;
using System.Collections;

public class FacingIndicator : MonoBehaviour 
{
	[SerializeField] public Renderer[] directions;
	[SerializeField] public Material normal;
	[SerializeField] public Material selected;
	
	public void SetDirection (Directions dir)
	{
		int index = (int)dir;
		for (int i = 0; i < 4; ++i)
			directions[i].material = (i == index) ? selected : normal;
	}
}
