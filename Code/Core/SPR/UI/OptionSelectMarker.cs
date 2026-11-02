using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI;

public class OptionSelectMarker : MonoBehaviour
{
	[SerializeField]
	private RectTransform indicator;

	private RectTransform rect;

	public RectTransform Rect => rect ?? (rect = GetComponent<RectTransform>());

	public void SetActive(bool value, bool instant = false)
	{
		indicator.gameObject.SetActive(value);
	}

	public async UniTask MoveToAsync(RectTransform optionRect, bool instant)
	{
		SetActive(value: true);
		Rect.DOKill();
		if (instant)
		{
			await UniTask.Yield(PlayerLoopTiming.LastTimeUpdate);
			Rect.position = new Vector2(Rect.position.x, optionRect.position.y);
		}
		else
		{
			await Rect.DOMoveY(optionRect.position.y, 0.3f).SetEase(Ease.OutCubic).WaitAsync();
		}
	}
}
