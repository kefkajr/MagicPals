using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour
{
	#region Notifications
	public const string EquippedNotification = "Inventory.EquippedNotification";
	public const string UnEquippedNotification = "Inventory.UnEquippedNotification";
	#endregion

	#region Fields / Properties
	// Items includes equipment
	public List<Merchandise> items { get { return _items; } }
	List<Merchandise> _items = new List<Merchandise>();

	public List<Equippable> equippableItems { get { return items.OfType<Equippable>().ToList(); }}
	public List<Equippable> equippedItems { get { return equippableItems.FindAll(equippable => equippable.isEquipped); } }
	#endregion

	#region Public

	public void Add (Merchandise item)
    {
		_items.Add(item);
    }

	public void Equip (Equippable equippable, EquipSlots slots)
	{
		if (equippable.isEquipped)
			return;

		UnEquip(slots);

		equippable.currentSlots = slots;

		equippable.isEquipped = true;

		this.PostNotification(EquippedNotification, equippable);
	}

	public void UnEquip (Equippable equippable)
	{
		if (!equippable.isEquipped)
			return;

		equippable.currentSlots = EquipSlots.None;

		equippable.isEquipped = false;

		this.PostNotification(UnEquippedNotification, equippable);
	}
	
	public void UnEquip (EquipSlots slots)
	{
		for (int i = equippedItems.Count - 1; i >= 0; --i)
		{
			Equippable equippable = equippedItems[i];
			if ( (equippable.currentSlots & slots) != EquipSlots.None )
				UnEquip(equippable);
		}
	}

	public Equippable GetEquipment (EquipSlots slots)
	{
		for (int i = equippableItems.Count - 1; i >= 0; --i)
		{
			Equippable equippable = equippableItems[i];
			if ( (equippable.currentSlots & slots) != EquipSlots.None )
				return equippable;
		}
		return null;
	}
	#endregion
}