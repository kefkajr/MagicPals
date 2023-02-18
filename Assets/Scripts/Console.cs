using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Console : MonoBehaviour
{

    public static Console Main { get; private set; }

    [SerializeField] public GameObject entryPrefab;
    [SerializeField] public GameObject textGroup;
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
    }

    public void Log(string s)
    {
        if (textGroup == null || !textGroup.activeInHierarchy)
            return;

        StartCoroutine(DisplayInConsole(s));
    }

    IEnumerator DisplayInConsole(string s)
    {
        var entry = Instantiate(entryPrefab);
        entry.transform.SetParent(textGroup.transform, false);
        var text = entry.GetComponent<Text>();
        text.text = string.Format("{0} ", Time.time) + s;
        

        if (textGroup.transform.childCount > maxMessageCount)
        {
            var difference = textGroup.transform.childCount - maxMessageCount;
            for (int i = 0; i < difference; i++)
            {

                Destroy(textGroup.transform.GetChild(i).gameObject);
            }
        }

        textGroup.SetActive(textGroup.transform.childCount > 0);

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
}
