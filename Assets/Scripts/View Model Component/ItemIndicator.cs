using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemIndicator : MonoBehaviour
{
    IEnumerator itemIndicatorRotateCoroutine;

	private void Awake()
	{
		itemIndicatorRotateCoroutine = RotateItemIndicator();
	}

	private void OnEnable()
    {
		StartCoroutine(itemIndicatorRotateCoroutine);
    }

	private void OnDisable()
	{
		StopCoroutine(itemIndicatorRotateCoroutine);
	}

	IEnumerator RotateItemIndicator()
	{
		while (gameObject.activeSelf)
		{
			transform.Rotate(Vector3.down);
			yield return null;
		}
	}

}
