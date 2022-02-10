using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Create new ability catalog recipe")]
public class AbilityCatalogRecipe : ScriptableObject 
{
	[System.Serializable]
	public class Category
	{
		public string name;
		public string[] entries;
	}
	public Category[] categories;
}