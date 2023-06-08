using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemIndicator : MonoBehaviour {
    IEnumerator itemIndicatorRotateCoroutine;

	private void Awake() {
		itemIndicatorRotateCoroutine = RotateItemIndicator();
	}

	private void OnEnable() {
		StartCoroutine(itemIndicatorRotateCoroutine);
    }

	private void OnDisable() {
		StopCoroutine(itemIndicatorRotateCoroutine);
	}

	IEnumerator RotateItemIndicator() {
		while (gameObject.activeSelf)
		{
			transform.Rotate(Vector3.down);
			yield return null;
		}
	}

	public void SetPosition(Tile tile) {
		Vector3 newPosition = tile.center;
		newPosition.y += 0.25f;
		transform.position = newPosition;
	}

}
