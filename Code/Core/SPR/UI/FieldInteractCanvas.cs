using System;
using Cysharp.Threading.Tasks;
using Library;
using TMPro;
using UnityEngine;

namespace SPR.UI;

public class FieldInteractCanvas : MonoBehaviour
{
	[SerializeField]
	private RectTransform shortFrame;

	[SerializeField]
	private RectTransform longFrame;

	[SerializeField]
	private LocaleText localeText;

	private RectTransform rectTransform;

	private RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	private void Initialize(float height, float posX = 0f)
	{
		ChangeHeight(height);
		ChangePosX(posX);
		SetActive(value: false);
	}

	public void ChangeHeight(float height)
	{
		Vector2 anchoredPosition = RectTransform.anchoredPosition;
		anchoredPosition.y = height;
		RectTransform.anchoredPosition = anchoredPosition;
	}

	public void ChangePosX(float posX)
	{
		Vector2 anchoredPosition = RectTransform.anchoredPosition;
		anchoredPosition.x = posX;
		RectTransform.anchoredPosition = anchoredPosition;
	}

	public void SetActive(bool value)
	{
		if (!shortFrame.IsNull() && !longFrame.IsNull())
		{
			if (localeText.GetComponent<TextMeshProUGUI>().preferredWidth > shortFrame.rect.width)
			{
				shortFrame.gameObject.SetActive(value: false);
				longFrame.gameObject.SetActive(value: true);
			}
			else
			{
				shortFrame.gameObject.SetActive(value: true);
				longFrame.gameObject.SetActive(value: false);
			}
		}
		if (RectTransform.lossyScale.x < 0f)
		{
			Vector3 localScale = RectTransform.localScale;
			RectTransform.localScale = new Vector3(localScale.x * -1f, localScale.y, localScale.z);
		}
		base.gameObject.SetActive(value);
	}

	public static async UniTask InstantiateCanvasAsync(Transform transform, Vector2 pos, Action<FieldInteractCanvas> onComplete)
	{
		FieldInteractCanvas fieldInteractCanvas = await AddressableHelper.InstantiateAsync<FieldInteractCanvas>("FieldInteractCanvas.prefab", transform);
		fieldInteractCanvas.Initialize(pos.y, pos.x);
		onComplete(fieldInteractCanvas);
	}
}
