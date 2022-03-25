using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DescriptionPanelController : MonoBehaviour
{
	#region Constants
	const string ShowKey = "Show";
	const string HideKey = "Hide";
	#endregion

	#region Fields / Properties
	[SerializeField] public Text titleLabel;
	[SerializeField] public Text descriptionLabel;
	[SerializeField] public Panel panel;
	[SerializeField] public GameObject canvas;
	#endregion

	#region MonoBehaviour
	void Start()
	{
		panel.SetPosition(HideKey, false);
		canvas.SetActive(false);
	}
	#endregion

	#region Public
	public void Show(Describable describable)
	{
		if (describable == null)
        {
			Hide();
			return;
        }

		canvas.SetActive(true);
		titleLabel.text = describable.name;
		descriptionLabel.text = FormatDescription(describable.description);

		TogglePos(ShowKey);
	}

	public void Hide()
	{
		Tweener t = TogglePos(HideKey);
		t.completedEvent += delegate (object sender, System.EventArgs e)
		{
			if (panel.CurrentPosition == panel[HideKey])
			{
				canvas.SetActive(false);
			}
		};
	}

	Tweener TogglePos(string pos)
	{
		Tweener t = panel.SetPosition(pos, true);
		t.duration = 0.5f;
		t.equation = EasingEquations.EaseOutQuad;
		return t;
	}


	string FormatDescription(string description)
    {
		return description.Replace("\\n", "\n");
	}
	#endregion
}
