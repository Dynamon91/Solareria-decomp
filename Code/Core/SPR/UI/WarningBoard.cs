using DG.Tweening;
using Library;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class WarningBoard : MonoBehaviour
{
	[SerializeField]
	private Image backgroundImage;

	[SerializeField]
	private LocaleText warningText;

	private const float FadeDuration = 3f;

	public void Initialize()
	{
		HideWarning();
	}

	public void ShowWarning(string localeKey)
	{
		base.gameObject.SetActive(value: true);
		backgroundImage.DOKill();
		backgroundImage.SetAlpha(1f);
		backgroundImage.DOAlpha(0f, 3f).SetEase(Ease.InQuart);
		TextMeshProUGUI tMP = warningText.TMP;
		tMP.DOKill();
		warningText.SetData(localeKey);
		tMP.gameObject.SetActive(value: true);
		tMP.alpha = 1f;
		tMP.DOFade(0f, 3f).SetEase(Ease.InQuart).OnComplete(delegate
		{
			base.gameObject.SetActive(value: false);
		});
		Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_NegativePopup");
	}

	public void HideWarning()
	{
		backgroundImage.DOKill();
		backgroundImage.SetAlpha(0f);
		TextMeshProUGUI tMP = warningText.TMP;
		tMP.DOKill();
		tMP.alpha = 0f;
		tMP.gameObject.SetActive(value: false);
		base.gameObject.SetActive(value: false);
	}
}
