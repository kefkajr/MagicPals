using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AwarenessLine : MonoBehaviour {
    public LineRenderer lineRenderer;
    public FloatingText awarenessLevelFloatingText;

    private Awareness awareness;

    public bool isFading;

    const float terminusOffset = 0.6f;
    const float midddleOffset = 2.5f;

    public void Update() {
        if (awareness == null) return;

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

    public void SetAwareness(Awareness awareness, Material material) {
        this.awareness = awareness;
        awarenessLevelFloatingText.Display(string.Format("{0}", awareness.level));
        lineRenderer.positionCount = 2;
        lineRenderer.material = material;

        // Make sure the line and text are visible when the awareness is set.
        Color startColor = new Color(lineRenderer.startColor.r, lineRenderer.startColor.g, lineRenderer.startColor.b, 1);

        lineRenderer.startColor = startColor;
        lineRenderer.endColor = startColor;
        awarenessLevelFloatingText.label.color = startColor;
    }

    public void Fadeout() {
        StartCoroutine(AnimateFadeout());
    }

    public void Clear() {
        Poolable p = GetComponent<Poolable>();
        GameObjectPoolController.Enqueue(p);
    }

    IEnumerator AnimateFadeout() {
        isFading = true;
        float fadeSpeed = 0.6f;
        float timeElapsed = 0f;
        float alpha = 1f;

        yield return new WaitForSeconds(1f);
 
        while (timeElapsed < fadeSpeed)
        {
            alpha = Mathf.Lerp(1f, 0f, timeElapsed / fadeSpeed);

            Color newStartColor = new Color(lineRenderer.startColor.r, lineRenderer.startColor.g, lineRenderer.startColor.b, alpha);
            Color newEndColor = new Color(lineRenderer.endColor.r, lineRenderer.endColor.g, lineRenderer.endColor.b, alpha);
            Color textColor = new Color(awarenessLevelFloatingText.label.color.r, awarenessLevelFloatingText.label.color.g, awarenessLevelFloatingText.label.color.b, alpha);
            lineRenderer.startColor = newStartColor;
            lineRenderer.endColor = newEndColor;
 
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        isFading = false;
        Clear();
    }
}
