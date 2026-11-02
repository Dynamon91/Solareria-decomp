using DG.Tweening;
using SPR.UI;
using UnityEngine;

namespace SPR;

public class SkipBoard : MonoBehaviour
{
	[SerializeField]
	private HoldInput holdInput;

	private CanvasGroup canvasGroup;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public void Initialize(float duration)
	{
		base.gameObject.SetActive(value: false);
		holdInput.Initialize(duration);
	}

	public void Activate()
	{
		base.gameObject.SetActive(value: true);
		CanvasGroup.DOKill();
		CanvasGroup.alpha = 0.5f;
		CanvasGroup.DOFade(0f, 1f).SetEase(Ease.Linear);
	}

	public bool CheckInput(bool input, bool ignoreTimescale = false)
	{
		if (input)
		{
			base.gameObject.SetActive(value: true);
			CanvasGroup.DOKill();
			CanvasGroup.alpha = 1f;
			CanvasGroup.DOFade(0f, 1f).SetEase(Ease.Linear);
		}
		return holdInput.CheckInput(input, ignoreTimescale);
	}

	public void Deactivate(bool instant = true)
	{
		CanvasGroup.DOKill();
		if (instant)
		{
			CanvasGroup.alpha = 0f;
			base.gameObject.SetActive(value: false);
		}
		else
		{
			CanvasGroup.DOFade(0f, 1f).OnComplete(delegate
			{
				base.gameObject.SetActive(value: false);
			});
		}
	}
}
