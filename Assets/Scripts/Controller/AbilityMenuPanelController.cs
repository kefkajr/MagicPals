﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class AbilityMenuPanelController : MonoBehaviour {
	#region Constants
	const string ShowKey = "Show";
	const string HideKey = "Hide";
	const string EntryPoolKey = "AbilityMenuPanel.Entry";
	const int MenuCount = 4;
	#endregion

	#region Fields / Properties
	[SerializeField] public GameObject entryPrefab;
	[SerializeField] public Text titleLabel;
	[SerializeField] public Panel panel;
	[SerializeField] public GameObject canvas;
	public List<AbilityMenuEntry> menuEntries = new List<AbilityMenuEntry>(MenuCount);
	public int selection { get; private set; }
	#endregion

	#region MonoBehaviour
	void Awake() {
		GameObjectPoolController.AddEntry(EntryPoolKey, entryPrefab, MenuCount, int.MaxValue);
	}

	void Start() {
		panel.SetPosition(HideKey, false);
		canvas.SetActive(false);
	}
	#endregion

	#region Public
	public void Show(string title, List<string> options) {
		canvas.SetActive(true);
		Clear();
		titleLabel.text = title;
		for (int i = 0; i < options.Count; ++i)
		{
			AbilityMenuEntry entry = Dequeue();
			entry.Title = options[i];
			menuEntries.Add(entry);
		}
		SetSelection(0);
		TogglePos(ShowKey);
	}

	public void Hide() {
		Tweener t = TogglePos(HideKey);
		t.completedEvent += delegate(object sender, System.EventArgs e) {
			if (panel.CurrentPosition == panel[HideKey]) {
				Clear();
				canvas.SetActive(false);
			}
		};
	}

	public void SetLocked(int index, bool value) {
		if (index < 0 || index >= menuEntries.Count)
			return;

		menuEntries[index].IsLocked = value;
		if (value && selection == index)
			Next();
	}

	public bool GetLocked(int index) {
		return menuEntries[index].IsLocked;
	}

	public void Next() {
		for (int i = selection + 1; i < selection + menuEntries.Count; ++i) {
			int index = i % menuEntries.Count;
			if (SetSelection(index))
				break;
		}
	}

	public void Previous() {
		for (int i = selection - 1 + menuEntries.Count; i > selection; --i) {
			int index = i % menuEntries.Count;
			if (SetSelection(index))
				break;
		}
	}

	public bool SetSelection(int value) {
		if (menuEntries[value].IsLocked)
			return false;
		
		// Deselect the previously selected entry
		if (selection >= 0 && selection < menuEntries.Count)
			Deselect();
		
		selection = value;
		
		// Select the new entry
		if (selection >= 0 && selection < menuEntries.Count)
			menuEntries[selection].IsSelected = true;
		
		return true;
	}

	public void Deselect() {
		menuEntries[selection].IsSelected = false;
	}
	
	#endregion

	#region Private
	AbilityMenuEntry Dequeue() {
		Poolable p = GameObjectPoolController.Dequeue(EntryPoolKey);
		AbilityMenuEntry entry = p.GetComponent<AbilityMenuEntry>();
		entry.SetController(this);
		entry.transform.SetParent(panel.transform, false);
		entry.transform.localScale = Vector3.one;
		entry.gameObject.SetActive(true);
		entry.Reset();
		return entry;
	}

	void Enqueue(AbilityMenuEntry entry) {
		Poolable p = entry.GetComponent<Poolable>();
		GameObjectPoolController.Enqueue(p);
	}

	void Clear() {
		for (int i = menuEntries.Count - 1; i >= 0; --i)
			Enqueue(menuEntries[i]);
		menuEntries.Clear();
	}

	Tweener TogglePos(string pos) {
		Tweener t = panel.SetPosition(pos, true);
		t.duration = 0.5f;
		t.equation = EasingEquations.EaseOutQuad;
		return t;
	}
	#endregion
}
