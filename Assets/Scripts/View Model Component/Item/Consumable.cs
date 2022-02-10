﻿using UnityEngine;
using System.Collections;

public class Consumable : Merchandise 
{
	public void Consume (GameObject target)
	{
		Feature[] features = GetComponentsInChildren<Feature>();
		for (int i = 0; i < features.Length; ++i)
			features[i].Apply(target);
	}
}