using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    [SerializeField] public GameObject flyawayTextPrefab;
    private const string flyawayTextPoolKey = "UIController.statChangeDecoratorPrefab";

    public void Setup(int unitCount) {
        GameObjectPoolController.AddEntry(flyawayTextPoolKey, flyawayTextPrefab, unitCount, int.MaxValue);
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
		
        DisplayFlyawayText(unit, differenceString);
	}

    public void DisplayFlyawayText(Unit unit, string s) {
        FloatingText text = Deqeue(unit.transform.position);
        text.Display(s);
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

    FloatingText Deqeue(Vector3 position) {
        Poolable p = GameObjectPoolController.Dequeue(flyawayTextPoolKey);
        p.transform.SetParent(transform, false);
        p.transform.localScale = Vector3.one;
        p.transform.position = position;
        p.gameObject.SetActive(true);

        var flyawayText = p.GetComponentInChildren<FloatingText>();
        return flyawayText;
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