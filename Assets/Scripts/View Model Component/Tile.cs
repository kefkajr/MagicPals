using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tile : MonoBehaviour 
{
	#region Const
	public const float stepHeight = 0.25f;
	#endregion

	#region Fields / Properties
	public Point pos;
	public int height;
	public Vector3 center { get { return new Vector3(pos.x, height * stepHeight, pos.y); }}
	public GameObject occupant;
	public List<Merchandise> items;

	public GameObject itemIndicator;
	IEnumerator itemIndicatorRotateCoroutine;

	[HideInInspector] public Tile prev;
	[HideInInspector] public int distance;
	#endregion

	#region Public
	public void Grow ()
	{
		height++;
		Match();
	}
	
	public void Shrink ()
	{
		height--;
		Match ();
	}

	public void Load (Point p, int h)
	{
		pos = p;
		height = h;
		Match();
	}
	
	public void Load (Vector3 v)
	{
		Load (new Point((int)v.x, (int)v.z), (int)v.y);
	}

	public void AddItem(Merchandise item, Inventory fromInventory)
    {
		items.Add(item);
		fromInventory.items.Remove(item);
		item.gameObject.transform.SetParent(this.transform);
		RefreshItemIndicator();
	}

	public void RemoveItem(Merchandise item, Inventory toInventory)
    {
		toInventory.items.Add(item);
		items.Remove(item);
		item.gameObject.transform.SetParent(toInventory.transform);
		RefreshItemIndicator();
	}

	public void RefreshItemIndicator()
    {
		if (items.Count > 0)
        {
			itemIndicator.SetActive(true);
			StartCoroutine(itemIndicatorRotateCoroutine);
        } else
        {
			itemIndicator.SetActive(false);
			StopCoroutine(itemIndicatorRotateCoroutine);
        }
    }
    #endregion

    #region Private
    private void Awake()
    {
		itemIndicatorRotateCoroutine = RotateItemIndicator();
    }

	IEnumerator RotateItemIndicator()
	{
		while (itemIndicator.activeSelf)
		{
			itemIndicator.transform.Rotate(Vector3.down, (30 * Time.deltaTime));
			yield return null;
		}
	}

	void Match ()
	{
		transform.localPosition = new Vector3( pos.x, height * stepHeight / 2f, pos.y );
		transform.localScale = new Vector3(1, height * stepHeight, 1);
	}
	#endregion
}
