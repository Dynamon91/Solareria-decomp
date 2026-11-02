using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI.Menu.Status;

public class HeatSkillIndicator : MonoBehaviour
{
	private CanvasGroup canvasGroup;

	private RectTransform rect;

	private static readonly Vector2 InitialPos = new Vector2(-300f, 0f);

	private const int Width = 200;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private RectTransform RectTransform => rect ?? (rect = GetComponent<RectTransform>());

	public bool IsActive { get; private set; }

	public bool CanControl { get; private set; }

	public void Initialize()
	{
		IsActive = false;
		CanControl = false;
		CanvasGroup.alpha = 0f;
		RectTransform.anchoredPosition = InitialPos;
	}

	public void SetActive(bool value)
	{
		IsActive = value;
		CanControl = false;
		if (value)
		{
			base.gameObject.SetActive(value: true);
		}
		CanvasGroup.DOAlpha(value ? 1 : 0, 1f / 6f).OnComplete(delegate
		{
			CanControl = true;
			if (!value)
			{
				base.gameObject.SetActive(value: false);
			}
		});
	}

	public async UniTaskVoid MoveAsync(int horizontal)
	{
		if (CanControl)
		{
			CanControl = false;
			Vector2 anchoredPosition = RectTransform.anchoredPosition;
			anchoredPosition.x += 200 * horizontal.GetSign();
			RectTransform.anchoredPosition = anchoredPosition;
			await UniTask.Yield();
			CanControl = true;
		}
	}
}
