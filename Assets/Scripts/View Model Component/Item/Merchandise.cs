using UnityEngine;
using System.Collections;

public class Merchandise : MonoBehaviour {
	public int buy;
	public int sell;

	public Describable describable { get { return GetComponent<Describable>(); } }
}