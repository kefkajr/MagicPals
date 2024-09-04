using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Create new unit recipe")]
public class SpawnRecipe : ScriptableObject 
{
	public string model;
	public string abilityCatalog;
	public string strategy;
	public Locomotions locomotion;
	public Alliances alliance;
	public PerceptionRecipe perceptionRecipe;
	public StatsTemplate statsTemplate;
}