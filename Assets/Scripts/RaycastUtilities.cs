using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public static class RaycastUtilities {
    public static bool IsPointerOverUIObject(Vector2 screenPos, GameObject GO) {
        var hitObjects = UIRaycast(ScreenPosToPointerData(screenPos));
        return hitObjects.Contains(GO);
    }

    public static bool IsPointerOverGameObject(Vector2 screenPos, GameObject GO) {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        var hits = Physics.RaycastAll(ray, 100);
        foreach(RaycastHit h in hits) {
            Debug.Log(h.collider.gameObject.name);
            if (h.collider.gameObject == GO) {
                return true;
            }
        }
        return false;
    }
 
    public static List<GameObject> UIRaycast(PointerEventData pointerData) {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        return results.Select(r => r.gameObject).ToList();
    }
 
    static PointerEventData ScreenPosToPointerData(Vector2 screenPos)
       => new(EventSystem.current) { position = screenPos };
}