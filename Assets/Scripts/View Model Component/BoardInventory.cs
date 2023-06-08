using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardInventory : Inventory {
	const string PrefabPoolKey = "BoardInventory.Prefab";
	const int MenuCount = 4;

	[SerializeField] public GameObject itemIndicatorPrefab;

	public Dictionary<Point, List<Merchandise>> itemsByPoint = new Dictionary<Point, List<Merchandise>>();
	public Dictionary<Merchandise, ItemIndicator> itemIndicators = new Dictionary<Merchandise, ItemIndicator>();

	void Awake() {
		GameObjectPoolController.AddEntry("BoardInventory.Prefab", itemIndicatorPrefab, MenuCount, int.MaxValue);
	}

	ItemIndicator Dequeue() {
		Poolable p = GameObjectPoolController.Dequeue(PrefabPoolKey);
		ItemIndicator i = p.GetComponent<ItemIndicator>();
		i.transform.SetParent(transform, false);
		i.gameObject.SetActive(true);
		return i;
	}

	void Enqueue(ItemIndicator entry) {
		Poolable p = entry.GetComponent<Poolable>();
		GameObjectPoolController.Enqueue(p);
	}

	public List<Merchandise> GetItemsByPoint(Point point) {
		List<Merchandise> itemsAtPoint;
		itemsByPoint.TryGetValue(point, out itemsAtPoint);
		if (itemsAtPoint == null)
			return new List<Merchandise>();
		return itemsAtPoint;
	}

	public void AddByTile(Merchandise item, Tile tile) {
		base.Add(item);
		Point point = tile.pos;
		List<Merchandise> itemsAtPoint;
		itemsByPoint.TryGetValue(point, out itemsAtPoint);
		if (itemsAtPoint == null)
			itemsByPoint[point] = new List<Merchandise>();
		itemsByPoint[point].Add(item);

		// Set the position of the item indicator to be that of the original inventory
		ItemIndicator itemIndicator = Dequeue();
		itemIndicator.SetPosition(tile);
		itemIndicators[item] = itemIndicator;
	}

	public void RemoveByPoint(Merchandise item, Point point) {
		base.Remove(item);
		List<Merchandise> itemsAtPoint = itemsByPoint[point];
		itemsAtPoint.Remove(item);

		Enqueue(itemIndicators[item]);

		if (itemsAtPoint.Count == 0)
			itemsByPoint[point] = null;
	}

}
