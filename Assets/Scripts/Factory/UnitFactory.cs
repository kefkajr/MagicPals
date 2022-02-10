using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public static class UnitFactory
{
	#region Public
	public static GameObject Create (string name, int level)
	{
		UnitRecipe recipe = Resources.Load<UnitRecipe>("Unit Recipes/" + name);
		if (recipe == null)
		{
			Debug.LogError("No Unit Recipe for name: " + name);
			return null;
		}
		return Create(recipe, level);
	}

	public static GameObject Create (UnitRecipe recipe, int level)
	{
		GameObject unitObject = InstantiatePrefab("Units/" + recipe.model);
		unitObject.name = recipe.name;
		unitObject.AddComponent<Unit>();
		AddStats(unitObject);
		AddLocomotion(unitObject, recipe.locomotion);
		unitObject.AddComponent<Status>();
		AddInventory(unitObject);
		AddJob(unitObject, recipe.job);
		AddRank(unitObject, level);
		unitObject.AddComponent<Health>();
		unitObject.AddComponent<Mana>();
		AddAttack(unitObject, recipe.attack);
		AddAbilityCatalog(unitObject, recipe.abilityCatalog);
		AddAlliance(unitObject, recipe.alliance);
		AddAttackPattern(unitObject, recipe.strategy);
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

	static void AddStats (GameObject unitObject)
	{
		Stats s = unitObject.AddComponent<Stats>();
		s.SetValue(StatTypes.LVL, 1, false);
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

	static void AddRank (GameObject unitObject, int level)
	{
		Rank rank = unitObject.AddComponent<Rank>();
		rank.Init(level);
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
		List <GameObject> items = new List<GameObject>();
		items.Add(CreateConsumable(inv, "Health Potion", StatTypes.HP, 300));
		items.Add(CreateConsumable(inv, "Bomb", StatTypes.HP, -150));
		items.Add(CreateEquipment(inv, "Sword", StatTypes.ATK, 10, EquipSlots.Primary));
		items.Add(CreateEquipment(inv, "Winged Helmet", StatTypes.MHP, 15, EquipSlots.Head));
		items.Add(CreateEquipment(inv, "Shield", StatTypes.DEF, 10, EquipSlots.Secondary));

		// Set items within inventory, and equip them if possible
		foreach(GameObject item in items)
        {
			item.transform.SetParent(invObject.transform);
			EquipItem(inv, item);
        }
	}

	static GameObject CreateItem(string title, StatTypes type, int amount)
	{
		GameObject item = new GameObject(title);
		StatModifierFeature smf = item.AddComponent<StatModifierFeature>();
		smf.type = type;
		smf.amount = amount;
		return item;
	}

	static GameObject CreateConsumable(Inventory inv, string title, StatTypes type, int amount)
	{
		GameObject item = CreateItem(title, type, amount);
		item.AddComponent<Consumable>();
		inv.Add(item.GetComponent<Consumable>());
		return item;
	}

	static GameObject CreateEquipment(Inventory inv, string title, StatTypes type, int amount, EquipSlots slot)
	{
		GameObject item = CreateItem(title, type, amount);
		Equippable equip = item.AddComponent<Equippable>();
		equip.defaultSlots = slot;
		inv.Add(item.GetComponent<Equippable>());
		return item;
	}

	static void EquipItem(Inventory inv, GameObject item)
	{
		Equippable toEquip = item.GetComponent<Equippable>();
		if (toEquip != null)
			inv.Equip(toEquip, toEquip.defaultSlots);
	}

	static void AddAttackPattern (GameObject obj, string name)
	{
		Driver driver = obj.AddComponent<Driver>();
		if (string.IsNullOrEmpty(name))
		{
			driver.normal = Drivers.Human;
		}
		else
		{
			driver.normal = Drivers.Computer;
			GameObject instance = InstantiatePrefab("Attack Pattern/" + name);
			instance.transform.SetParent(obj.transform);
		}
	}
	#endregion
}