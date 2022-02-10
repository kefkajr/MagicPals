using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Equippable : Merchandise
{
	#region Fields
	/// <summary>
	/// The EquipSlots flag which is the default
	/// equip location(s) for this item.
	/// For example, a normal weapon would only specify 
	/// primary, but a two-handed weapon would specify
	/// both primary and secondary.
	/// </summary>
	public EquipSlots defaultSlots;

	/// <summary>
	/// Some equipment may be allowed to be equipped in
	/// more than one slot location, such as when 
	/// dual-wielding swords.
	/// </summary>
	public EquipSlots secondarySlots;

	/// <summary>
	/// The slot(s) where an item is currently equipped
	/// </summary>
	public EquipSlots currentSlots;

	bool _isEquipped;
	[SerializeField]
	public bool isEquipped
	{
		get { return _isEquipped;}
		set
		{
			_isEquipped = value;
			if (value)
			{
				OnEquip();
			}
			else
			{
				OnUnEquip();
			}
		}
	}
	#endregion

	void OnEquip()
	{
		Feature[] features = GetComponentsInChildren<Feature>();
		for (int i = 0; i < features.Length; ++i)
		{
			features[i].Activate(gameObject);
			gameObject.name = gameObject.name + "*"; // Add marker
		}
	}
	void OnUnEquip()
	{
		Feature[] features = GetComponentsInChildren<Feature>();
		for (int i = 0; i < features.Length; ++i)
		{
			features[i].Deactivate();
			gameObject.name = gameObject.name.Remove(gameObject.name.Length-1); // Remove marker
		}
	}
}