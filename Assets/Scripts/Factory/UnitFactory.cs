using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public static class UnitFactory
{
	#region Public
	public static GameObject Create (string name)
	{
		UnitRecipe recipe = Resources.Load<UnitRecipe>("Unit Recipes/" + name);
		if (recipe == null)
		{
			Debug.LogError("No Unit Recipe for name: " + name);
			return null;
		}
		return Create(recipe);
	}

	public static GameObject Create (UnitRecipe recipe)
	{
		GameObject unitObject = InstantiatePrefab("Units/" + recipe.model);
		unitObject.name = recipe.name;
		unitObject.AddComponent<Unit>();
		AddStats(unitObject, recipe.statsTemplate);
		AddLocomotion(unitObject, recipe.locomotion);
		unitObject.AddComponent<Status>();
		// AddJob(unitObject, recipe.job);
		// AddRank(unitObject); 
		unitObject.AddComponent<Health>();
		unitObject.AddComponent<Mana>();
		AddAttack(unitObject, recipe.attack);
		AddAbilityCatalog(unitObject, recipe.abilityCatalog);
		AddAlliance(unitObject, recipe.alliance);
		AddAttackPattern(unitObject, recipe.attackPattern);
		AddInventory(unitObject);

		AddAwareness(unitObject, recipe.perceptionRecipe);
		return unitObject;
	}
	#endregion

	#region Private
	static GameObject InstantiatePrefab (string name)
	{
		GameObject prefab = Resources.Load<GameObject>(name);
		if (prefab == null)
		{
			Debug.LogError("No Prefab for name: " + name);
			return new GameObject(name);
		}
		GameObject instance = GameObject.Instantiate(prefab);
		instance.name = instance.name.Replace("(Clone)", "");
		return instance;
	}

	static void AddStats (GameObject unitObject, StatsTemplate template)
	{
		Stats s = unitObject.AddComponent<Stats>();
		s.InitializeWithTemplate(template);
	}

	static void AddJob (GameObject unitObject, string name)
	{
		GameObject instance = InstantiatePrefab("Jobs/" + name);
		instance.transform.SetParent(unitObject.transform);
		Job job = instance.GetComponent<Job>();
		job.Employ();
		job.LoadDefaultStats();
	}

	static void AddLocomotion (GameObject unitObject, Locomotions type)
	{
		switch (type)
		{
		case Locomotions.Walk:
			unitObject.AddComponent<WalkMovement>();
			break;
		case Locomotions.Fly:
			unitObject.AddComponent<FlyMovement>();
			break;
		case Locomotions.Teleport:
			unitObject.AddComponent<TeleportMovement>();
			break;
		}
	}

	static void AddAlliance (GameObject unitObject, Alliances type)
	{
		Alliance alliance = unitObject.AddComponent<Alliance>();
		alliance.type = type;
	}

	static void AddRank (GameObject unitObject)
	{
		Rank rank = unitObject.AddComponent<Rank>();
		rank.Init(1);
	}

	static void AddAttack (GameObject unitObject, string name)
	{
		GameObject instance = InstantiatePrefab("Abilities/" + name);
		instance.transform.SetParent(unitObject.transform);
	}

	static void AddAbilityCatalog (GameObject unitObject, string name)
	{
		GameObject main = new GameObject("Ability Catalog");
		main.transform.SetParent(unitObject.transform);
		main.AddComponent<AbilityCatalog>();

		AbilityCatalogRecipe recipe = Resources.Load<AbilityCatalogRecipe>("Ability Catalog Recipes/" + name);
		if (recipe == null)
		{
			Debug.LogError("No Ability Catalog Recipe Found: " + name);
			return;
		}

		for (int i = 0; i < recipe.categories.Length; ++i)
		{
			GameObject category = new GameObject( recipe.categories[i].name );
			category.transform.SetParent(main.transform);

			for (int j = 0; j < recipe.categories[i].entries.Length; ++j)
			{
				string abilityName = string.Format("Abilities/{0}/{1}", recipe.categories[i].name, recipe.categories[i].entries[j]);
				GameObject ability = InstantiatePrefab(abilityName);
				ability.transform.SetParent(category.transform);
			}
		}
	}

	static void AddInventory(GameObject unitObject)
    {
		// Create inventory object and component
		GameObject invObject = new GameObject("Inventory");
		invObject.AddComponent<Inventory>();
		Inventory inv = invObject.GetComponent<Inventory>();
		invObject.transform.SetParent(unitObject.transform);

		// Create item objects

		// TODO Instead of this, instantiate the prefabs

		List <GameObject> items = new List<GameObject>();
		// items.Add(InstantiatePrefab("Merchandise/" + "Sword"));
		// items.Add(InstantiatePrefab("Merchandise/" + "Shield"));
		// items.Add(InstantiatePrefab("Merchandise/" + "Winged Helmet"));
		// items.Add(InstantiatePrefab("Merchandise/" + "Health Potion"));
		// items.Add(InstantiatePrefab("Merchandise/" + "Bomb"));

        // Set items within inventory, and equip them if possible
        foreach (GameObject item in items)
        {
            item.transform.SetParent(invObject.transform);
			Merchandise merchandise = item.GetComponent<Merchandise>();
			inv.Add(merchandise);
            EquipItem(inv, item);
        }
    }

	static void EquipItem(Inventory inv, GameObject item)
	{
		Equippable toEquip = item.GetComponentInChildren<Equippable>();
		if (toEquip != null)
			inv.Equip(toEquip, toEquip.defaultSlots);
	}

	static void AddAttackPattern (GameObject obj, string name)
	{
		Driver driver = obj.AddComponent<Driver>();
		if (string.IsNullOrEmpty(name))
		{
			driver.normal = DriverType.Human;
		}
		else
		{
			driver.normal = DriverType.Computer;
			GameObject instance = InstantiatePrefab("Attack Pattern/" + name);
			instance.transform.SetParent(obj.transform);
		}
	}

	static void AddAwareness(GameObject obj, PerceptionRecipe perceptionRecipe)
	{
		Stealth stealth = obj.AddComponent<Stealth>();
		Perception perception = obj.AddComponent<Perception>();
		perception.viewingRange = perceptionRecipe.viewingRange;
		perception.hearingRange = perceptionRecipe.hearingRange;
	}
	#endregion
}