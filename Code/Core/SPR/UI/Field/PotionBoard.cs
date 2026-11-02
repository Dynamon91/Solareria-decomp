using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using TMPro;
using UnityEngine;

namespace SPR.UI.Field;

public class PotionBoard : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI potionCountText;

	private RectTransform rect;

	private CanvasGroup canvasGroup;

	private Animator animator;

	private Sequence showHideSequence;

	private RectTransform Rect => rect ?? (rect = GetComponent<RectTransform>());

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	public void Initialize(int max)
	{
		UpdatePotionBoard(max, isReset: true);
		HideAsync(0f).Forget();
	}

	public void UpdatePotionBoard(int current, bool isReset = false)
	{
		potionCountText.text = current.ToString();
	}

	public void StartAnimation()
	{
		Animator.Play("Potion Board_activate");
	}

	public void CancelAnimation()
	{
		Animator.Play("Potion Board_idle");
	}

	public async UniTask ShowAsync(float duration)
	{
		showHideSequence?.Kill();
		base.gameObject.SetActive(value: true);
		if (duration <= 0f)
		{
			Rect.localScale = Vector3.one;
			CanvasGroup.alpha = 1f;
		}
		else if (!CanvasGroup.alpha.IsAlmostSame(1f))
		{
			Rect.localScale = Vector3.one * 1.1f;
			CanvasGroup.alpha = 0f;
			showHideSequence = DOTween.Sequence();
			showHideSequence.Append(Rect.DOScale(Vector3.one, duration)).Join(CanvasGroup.DOFade(1f, duration));
			await showHideSequence.WaitAsync();
		}
	}

	public async UniTask HideAsync(float duration)
	{
		showHideSequence?.Kill();
		if (duration <= 0f)
		{
			Rect.localScale = Vector3.one * 1.1f;
			CanvasGroup.alpha = 0f;
			base.gameObject.SetActive(value: false);
		}
		else if (CanvasGroup.alpha.IsAlmostSame(0f))
		{
			base.gameObject.SetActive(value: false);
		}
		else
		{
			Rect.localScale = Vector3.one;
			CanvasGroup.alpha = 1f;
			showHideSequence = DOTween.Sequence();
			showHideSequence.Append(Rect.DOScale(Vector3.one * 1.1f, duration)).Join(CanvasGroup.DOFade(0f, duration));
			await showHideSequence.WaitAsync();
		}
	}
}
