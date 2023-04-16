#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Console : MonoBehaviour
{

    public static Console Main { get; private set; }

    const string ShowKey = "Show";
	const string HideKey = "Hide";
    const string EntryPoolKey = "Console.Log";

    [SerializeField] public GameObject entryPrefab;
    [SerializeField] public Panel panel;
    public int maxMessageCount = 5;

    private void Awake()
    {
        if (Main != null && Main != this)
        {
            Destroy(this);
        }
        else
        {
            Main = this;
        }

		GameObjectPoolController.AddEntry(EntryPoolKey, entryPrefab, maxMessageCount, int.MaxValue);
    }

    public void Log(string s)
    {
        if (panel == null || !panel.gameObject.activeInHierarchy)
            return;

        StartCoroutine(DisplayInConsole(s));
    }

    IEnumerator DisplayInConsole(string s)
    {
        Poolable entry = GameObjectPoolController.Dequeue(EntryPoolKey);
        entry.transform.SetParent(panel.transform, false);
		entry.transform.localScale = Vector3.one;
		entry.gameObject.SetActive(true);
        var text = entry.GetComponent<Text>();
        text.text = string.Format("{0} ", Time.time) + s;

        if (panel.transform.childCount > maxMessageCount)
        {
            var difference = panel.transform.childCount - maxMessageCount;
            for (int i = 0; i < difference; i++)
            {

                Poolable p = panel.transform.GetChild(i).GetComponent<Poolable>();
                GameObjectPoolController.Enqueue(p);
            }
        }

        if (panel.transform.childCount > 0)
            TogglePos(ShowKey);

        StartCoroutine(PulseLogEntry(text));
        yield return null;
    }

    IEnumerator PulseLogEntry(Text text)
    {
        Color pulseColor = Color.yellow;
        Color normalColor = Color.white;

        text.color = pulseColor;
        float time = 0;
        float duration = 2f;

        while (time < duration)
        {
            var newColor = Color.Lerp(pulseColor, normalColor, time / duration);
            text.color = newColor;
            time += Time.deltaTime;
            yield return null;
        }
    }

    Tweener TogglePos (string pos)
	{
		Tweener t = panel.SetPosition(pos, true);
		t.duration = 0.5f;
		t.equation = EasingEquations.EaseOutQuad;
		return t;
	}
}
