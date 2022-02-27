using UnityEngine;
using System.Collections.Generic;

public class AbilityItemCost : MonoBehaviour 
{
	#region Fields
	public List<ItemCostDescriptor> itemCostDescriptors;
	Ability owner;
	Inventory inventory;
	#endregion

	#region MonoBehaviour
	void Awake ()
	{
		owner = GetComponent<Ability>();
	}

	// Should be run after object heirarchy is set by UnitFactory
    private void Start()
    {
		Transform abilityCategoryTransform = owner.gameObject.transform.parent;
		Transform abilityCatalogTransform = abilityCategoryTransform.parent;
		Transform unitTransform = abilityCatalogTransform.parent;
		inventory = unitTransform.GetComponentInChildren<Inventory>();
	}

    void OnEnable ()
	{
		this.AddObserver(OnCanPerformCheck, Ability.CanPerformCheck, owner);
		this.AddObserver(OnDidPerformNotification, Ability.DidPerformNotification, owner);
	}

	void OnDisable ()
	{
		this.RemoveObserver(OnCanPerformCheck, Ability.CanPerformCheck, owner);
		this.RemoveObserver(OnDidPerformNotification, Ability.DidPerformNotification, owner);
	}
	#endregion

	#region Notification Handlers
	void OnCanPerformCheck (object sender, object args)
	{
		bool canPerform = true;
		for (int i = 0; i < itemCostDescriptors.Count; ++i)
		{
			ItemCostDescriptor icd = itemCostDescriptors[i];
			int numberOfItem = 0;
			for (int ii = 0; ii < inventory.items.Count; ++ii)
            {
				Merchandise item = inventory.items[ii];
				if (item.gameObject.name == icd.itemName || item.gameObject.name == (icd.itemName + Equippable.IsEquippedMarker))
					numberOfItem++;
            }

			if (numberOfItem < icd.itemAmount)
			{
				canPerform = false;
				break;
			}
		}

		if(!canPerform)
        {
			BaseAdjustment adj = (BaseAdjustment)args;
			adj.FlipToggle();
        }
	}

	void OnDidPerformNotification(object sender, object args)
	{
		for (int i = 0; i < itemCostDescriptors.Count; ++i)
		{
			ItemCostDescriptor icd = itemCostDescriptors[i];
			int numberOfItemsToConsume = icd.itemAmount;
			for (int ii = inventory.items.Count - 1; ii > 0; --ii)
			{
				Merchandise item = inventory.items[ii];
				if (item.gameObject.name == icd.itemName || item.gameObject.name == (icd.itemName + Equippable.IsEquippedMarker))
                {
					Consumable consumable = item.GetComponentInChildren<Consumable>();
					if (consumable != null)
						consumable.Consume();

					numberOfItemsToConsume--;
                }

				if (numberOfItemsToConsume == 0)
					break;
			}
		}
	}
	#endregion
}