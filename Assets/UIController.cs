using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    [SerializeField] public GameObject floatingTextPrefab;
    private const string floatingTextPoolKey = "UIController.statChangeDecoratorPrefab";

    public void Setup(int unitCount) {
        GameObjectPoolController.AddEntry(floatingTextPoolKey, floatingTextPrefab, unitCount, int.MaxValue);
    }

    void OnEnable ()
	{
		this.AddObserver(OnHPDidChangeNotification, Stats.DidChangeNotification(StatTypes.HP));
	}
	
	void OnDisable ()
	{
		this.RemoveObserver(OnHPDidChangeNotification, Stats.DidChangeNotification(StatTypes.HP));
	}

    // Floating Text for HP changes

    void OnHPDidChangeNotification (object sender, object args)
	{
		Stats stats = sender as Stats;
        int newHPValue = stats[StatTypes.HP];
        int oldHPValue = (int)args;

        int difference = newHPValue - oldHPValue;
        string differenceString = difference.ToString();

        Unit unit = stats.GetComponentInParent<Unit>();
		
        DisplayFloatingText(unit, differenceString);
	}

    public void DisplayFloatingText(Unit unit, string s) {
        TMP_Text text = Deqeue(unit.transform.position);
        text.text = s;

        StartCoroutine(AnimateDecoratorDisplay(text));
    }

    // public Vector3 GetDecoratorPosition(Unit unit) {
    //     float offsetPosY = unit.transform.position.y + 1.5f;
 
    //     // Final position of marker above GO in world space
    //     Vector3 offsetPos = new Vector3(unit.transform.position.x, offsetPosY, unit.transform.position.z);
        
    //     // Calculate *screen* position (note, not a canvas/recttransform position)
    //     Vector2 canvasPos;
    //     Vector2 screenPoint = Camera.main.WorldToScreenPoint(offsetPos);
        
    //     // Convert screen position to Canvas / RectTransform space <- leave camera null if Screen Space Overlay
    //     RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out canvasPos);

    //     return canvasPos;
    // }

    TMP_Text Deqeue(Vector3 position) {
        Poolable p = GameObjectPoolController.Dequeue(floatingTextPoolKey);
        p.transform.SetParent(transform, false);
        p.transform.localScale = Vector3.one;
        p.transform.position = position;
        p.gameObject.SetActive(true);

        TMP_Text t = p.GetComponentInChildren<TMP_Text>();
        return t;
    }

    IEnumerator AnimateDecoratorDisplay(TMP_Text text) {
        yield return new WaitForSeconds(3f);
        Enqueue(text);
    }

    void Enqueue(TMP_Text t) {
        Poolable p = t.GetComponentInParent<Poolable>();
        GameObjectPoolController.Enqueue(p);
    }
}