using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Ending;

public class CreditText : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI nameText;

	[SerializeField]
	private TextMeshProUGUI contentText;

	[SerializeField]
	private RectTransform nameParent;

	[SerializeField]
	private RectTransform contentParent;

	private RectTransform rect;

	private RectTransform RectTransform => rect ?? (rect = GetComponent<RectTransform>());

	public void SetData(string nameValue, string contentValue)
	{
		nameText.text = nameValue;
		contentText.text = contentValue;
		Vector2 sizeDelta = RectTransform.sizeDelta;
		sizeDelta.y = 50f;
		RectTransform.sizeDelta = sizeDelta;
		LayoutRebuilder.ForceRebuildLayoutImmediate(nameText.rectTransform);
		LayoutRebuilder.ForceRebuildLayoutImmediate(contentText.rectTransform);
		LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent);
		UpdateAnchoredPosition();
	}

	public void SetNext(string nameValue, string contentValue)
	{
		Object.Instantiate(nameText, nameParent).text = nameValue;
		TextMeshProUGUI textMeshProUGUI = Object.Instantiate(contentText, contentParent);
		textMeshProUGUI.text = contentValue;
		Vector2 sizeDelta = RectTransform.sizeDelta;
		sizeDelta.y += 50f;
		RectTransform.sizeDelta = sizeDelta;
		LayoutRebuilder.ForceRebuildLayoutImmediate(textMeshProUGUI.rectTransform);
		LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent);
	}

	private void UpdateAnchoredPosition()
	{
		if (string.IsNullOrEmpty(contentText.text))
		{
			Vector2 pivot = nameParent.pivot;
			pivot.x = 0.5f;
			nameParent.pivot = pivot;
			Vector2 anchoredPosition = nameParent.anchoredPosition;
			anchoredPosition.x = 0f;
			nameParent.anchoredPosition = anchoredPosition;
		}
	}
}
