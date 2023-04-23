using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public Transform rig;
    public TMP_Text label;

    void Update()
    {
        rig.transform.LookAt(Camera.main.transform);
    }
}
