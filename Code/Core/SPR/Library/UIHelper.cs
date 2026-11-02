using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.Library;

public static class UIHelper
{
	public static Tween DOScrollXToTarget(this RectTransform rect, RectTransform target, RectTransform maskRectTransform, CanvasScaler canvasScaler, float duration, float padding = 0f)
	{
		Vector3[] array = new Vector3[4];
		Vector3[] array2 = new Vector3[4];
		target.GetWorldCorners(array);
		maskRectTransform.GetWorldCorners(array2);
		float num = canvasScaler.referenceResolution.x / (float)Screen.width;
		float num2 = array[2].x * num;
		float num3 = array[0].x * num;
		float num4 = array2[2].x * num - padding;
		float num5 = array2[0].x * num + padding;
		float num6 = ((num2 > num4) ? (num2 - num4) : ((!(num3 < num5)) ? 0f : (num3 - num5)));
		if (num6 == 0f)
		{
			return null;
		}
		return rect.DOAnchorPosX(rect.anchoredPosition.x - num6, duration);
	}

	public static void ScrollXToTarget(this RectTransform rect, RectTransform target, RectTransform maskRectTransform, float padding = 0f)
	{
		Vector3[] array = new Vector3[4];
		Vector3[] array2 = new Vector3[4];
		target.GetWorldCorners(array);
		maskRectTransform.GetWorldCorners(array2);
		float x = array[2].x;
		float x2 = array[0].x;
		float num = array2[2].x - padding;
		float num2 = array2[0].x + padding;
		float num3 = ((x > num) ? (x - num) : ((!(x2 < num2)) ? 0f : (x2 - num2)));
		if (num3 != 0f)
		{
			Vector2 anchoredPosition = rect.anchoredPosition;
			rect.anchoredPosition = new Vector2(anchoredPosition.x - num3, anchoredPosition.y);
		}
	}

	public static void ScrollYToTarget(this RectTransform rect, RectTransform target, float min, float max)
	{
		float num = rect.anchoredPosition.y + target.anchoredPosition.y;
		float num2 = Mathf.Clamp(num, min, max) - num;
		Vector2 anchoredPosition = rect.anchoredPosition;
		rect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y + num2);
	}

	public static async UniTask RebuildLayoutAfterFramesAsync(RectTransform layoutRoot, int frameCount = 1, Action onComplete = null)
	{
		Canvas.ForceUpdateCanvases();
		LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
		for (int i = 0; i < frameCount; i++)
		{
			await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
			Canvas.ForceUpdateCanvases();
			LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
		}
		onComplete?.Invoke();
	}
}
