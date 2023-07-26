using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AwarenessLine : MonoBehaviour {
    public LineRenderer lineRenderer;
    public FloatingText awarenessLevelFloatingText;

    private Awareness awareness;

    const float terminusOffset = 0.6f;
    const float midddleOffset = 2.5f;

    public void SetAwareness(Awareness awareness, Material material) {
        awarenessLevelFloatingText.Display(string.Format("{0}", awareness.level));
        lineRenderer.positionCount = 2;
        lineRenderer.material = material;

        Vector3 start = awareness.perception.unit.transform.position;
        start.y += terminusOffset;

        Vector3 end = awareness.stealth.unit.transform.position;
        end.y += terminusOffset;

        Vector3 middle = Vector3.Lerp(start, end, 0.5f);
        middle.y +=  midddleOffset;

        lineRenderer.positionCount = 200;
        float t = 0f;
        Vector3 B = new Vector3(0, 0, 0);
        for (int i = 0; i < lineRenderer.positionCount; i++) {
            B = (1 - t) * (1 - t) * start + 2 * (1 - t) * t * middle + t * t * end;
            lineRenderer.SetPosition(i, B);
            t += (1 / (float)lineRenderer.positionCount);
            if (i == lineRenderer.positionCount / 2) {
                Vector3 levelTextPosition = new Vector3(B.x, B.y + 1, B.z);
                awarenessLevelFloatingText.container.transform.position = levelTextPosition;
            }
        }
    }
}
