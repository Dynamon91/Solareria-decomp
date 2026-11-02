using DG.Tweening;
using Library;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

[RequireComponent(typeof(ScrollRect))]
[DisallowMultipleComponent]
public class ScrollBoard : MonoBehaviour
{
	[Header("Scroll Board Settings")]
	[SerializeField]
	private RectTransform viewport;

	[SerializeField]
	private RectTransform contentGroup;

	[Tooltip("작을수록 원하는 타겟이 뷰포트 안으로 들어오는 속도가 빨라집니다.")]
	[SerializeField]
	private float scrollDuration = 0.15f;

	private ScrollRect scrollRect;

	private ScrollRect ScrollRect => scrollRect ?? (scrollRect = GetComponent<ScrollRect>());

	public void ScrollIfTargetIsOutOfViewport(RectTransform targetRect, float padding = 0f, bool instant = false)
	{
		Vector3[] array = new Vector3[4];
		Vector3[] array2 = new Vector3[4];
		targetRect.GetWorldCorners(array);
		viewport.GetWorldCorners(array2);
		float y = array[1].y;
		float y2 = array[0].y;
		float num = array2[1].y - padding;
		float num2 = array2[0].y + padding;
		float height = contentGroup.rect.height;
		float height2 = viewport.rect.height;
		float num3 = ((y > num) ? (y - num) : ((!(y2 < num2)) ? 0f : (y2 - num2)));
		if (num3 != 0f)
		{
			float num4 = num3 / (height - height2);
			float num5 = Mathf.Clamp01(ScrollRect.verticalNormalizedPosition + num4);
			if (instant)
			{
				ScrollRect.verticalNormalizedPosition = num5;
			}
			else
			{
				ScrollRect.DOScroll(num5, scrollDuration).SetEase(Ease.InOutQuad);
			}
		}
	}
}
