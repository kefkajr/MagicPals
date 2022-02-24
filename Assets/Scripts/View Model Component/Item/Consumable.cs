using UnityEngine;
using System.Collections;

public class Consumable : MonoBehaviour
{
	public void Consume ()
	{
		Merchandise merchandise = GetComponentInParent<Merchandise>();
		Inventory inventory = merchandise.GetComponentInParent<Inventory>();
		inventory.Discard(merchandise);
	}
}