using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FlyawayText : MonoBehaviour
{
    public Transform container;
    public TMP_Text label;

    void Update()
    {
        container.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
    }

    public void Display(string s) {
        label.text = s;
    }

    public void DidCompleteFloatingAnimation() {
        Poolable p = GetComponentInParent<Poolable>();
        GameObjectPoolController.Enqueue(p);
    }
}
