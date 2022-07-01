using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Create new unit recipe")]
public class UnitRecipe : ScriptableObject 
{
	public string model;
	public string job;
	public string attack;
	public string abilityCatalog;
	public string strategy;
	public Locomotions locomotion;
	public Alliances alliance;
	public AwarenessRecipe awarenessRecipe;
}