using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public static class RaycastUtilities
{
    public static bool PointerIsOverObject(Vector2 screenPos, GameObject GO)
    {
        var hitObjects = UIRaycast(ScreenPosToPointerData(screenPos));
        return hitObjects.Contains(GO);
    }
 
    public static List<GameObject> UIRaycast(PointerEventData pointerData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        foreach(RaycastResult r in results) {
            Debug.Log(r.gameObject);
        }
        return results.Select(r => r.gameObject).ToList();
    }
 
    static PointerEventData ScreenPosToPointerData(Vector2 screenPos)
       => new(EventSystem.current) { position = screenPos };
}