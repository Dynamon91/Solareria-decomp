using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI.Fade;

public class LoadingBoard : MonoBehaviour
{
	[SerializeField]
	private LocaleText tipText;

	private LoadingTipSelector tipSelector;

	private CanvasGroup canvasGroup;

	private LoadingTipSelector TipSelector => tipSelector ?? (tipSelector = new LoadingTipSelector());

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public void Initialize()
	{
		CanvasGroup.alpha = 0f;
		base.gameObject.SetActive(value: false);
	}

	public async UniTask ShowAsync(IAttackSource attackSource)
	{
		TipName nextTip = TipSelector.GetNextTip(attackSource);
		if (nextTip == TipName.None)
		{
			DebugLog.GeneralLogWarning("[LoadingBoard] No tips available. Hiding LoadingBoard.");
			base.gameObject.SetActive(value: false);
			return;
		}
		string data = $"Tip_{nextTip}";
		tipText.SetData(data);
		base.gameObject.SetActive(value: true);
		CanvasGroup.DOKill();
		await CanvasGroup.DOFade(1f, 0.6f).WaitAsync();
	}

	public async UniTask HideAsync(float duration, float predelay = 0f)
	{
		CanvasGroup.DOKill();
		if (predelay > 0f)
		{
			await UniTask.WaitForSeconds(predelay);
		}
		await CanvasGroup.DOFade(0f, duration).WaitAsync();
		base.gameObject.SetActive(value: false);
	}
}
