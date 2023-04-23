using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public TMP_Text label;

    void Update()
    {
        transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
    }

    public void Display(string s) {
        label.text = s;
    }

    public void DidCompleteFloatingAnimation() {
        Poolable p = GetComponent<Poolable>();
        GameObjectPoolController.Enqueue(p);
    }
}
