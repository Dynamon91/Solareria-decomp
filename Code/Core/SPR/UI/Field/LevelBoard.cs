using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Field;

public class LevelBoard : MonoBehaviour
{
	[SerializeField]
	private Image expBackgroundImage;

	[SerializeField]
	private Image fillImage;

	[SerializeField]
	private TextMeshProUGUI levelText;

	[Header("Color Settings")]
	[SerializeField]
	private Color normalExpBackgroundColor;

	[SerializeField]
	private Color maxExpBackgroundColor;

	[SerializeField]
	private Color emptyColor;

	[SerializeField]
	private Color fullColor;

	[Space]
	[SerializeField]
	private LevelUpEffectProducer levelUpEffectProducer;

	private CanvasGroup canvasGroup;

	private RectTransform rect;

	private Sequence showHideSequence;

	public bool IsLevelUpProducing => levelUpEffectProducer.IsProducing;

	private static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	public void Initialize()
	{
		if ((object)canvasGroup == null)
		{
			canvasGroup = GetComponent<CanvasGroup>();
		}
		if ((object)rect == null)
		{
			rect = GetComponent<RectTransform>();
		}
		HideAsync(0f).Forget();
		UpdateLevelBoard();
		levelUpEffectProducer.Initialize();
	}

	public void UpdateLevelBoard()
	{
		UpdateExpBar();
		UpdateLevelText();
	}

	public void UpdateExpBar()
	{
		float num = PlayerStat.CurrentExp;
		float num2 = PlayerStat.MaxExp;
		float num3 = Mathf.Clamp01(num / num2);
		fillImage.fillAmount = num3;
		fillImage.color = Color.Lerp(emptyColor, fullColor, num3);
		expBackgroundImage.color = (PlayerStat.IsMaxLevel ? maxExpBackgroundColor : normalExpBackgroundColor);
	}

	private void UpdateLevelText()
	{
		levelText.text = PlayerStat.Level.ToString();
	}

	public void StartLevelUp(int levelDiff)
	{
		levelUpEffectProducer.SetData(levelDiff);
		levelUpEffectProducer.PlayEffect().Forget();
	}

	public void ShowLevelUp()
	{
		UpdateLevelText();
		levelUpEffectProducer.ShowItemBoard();
	}

	public void EndLevelUp()
	{
		UpdateExpBar();
	}

	public async UniTask ShowAsync(float duration)
	{
		showHideSequence?.Kill();
		base.gameObject.SetActive(value: true);
		if (duration <= 0f)
		{
			rect.localScale = Vector3.one;
			canvasGroup.alpha = 1f;
		}
		else if (!canvasGroup.alpha.IsAlmostSame(1f))
		{
			rect.localScale = Vector3.one * 1.1f;
			canvasGroup.alpha = 0f;
			showHideSequence = DOTween.Sequence();
			showHideSequence.Append(rect.DOScale(Vector3.one, duration)).Join(canvasGroup.DOFade(1f, duration));
			await showHideSequence.WaitAsync();
		}
	}

	public async UniTask HideAsync(float duration)
	{
		showHideSequence?.Kill();
		if (duration <= 0f)
		{
			rect.localScale = Vector3.one * 1.1f;
			canvasGroup.alpha = 0f;
			base.gameObject.SetActive(value: false);
		}
		else if (canvasGroup.alpha.IsAlmostSame(0f))
		{
			base.gameObject.SetActive(value: false);
		}
		else
		{
			rect.localScale = Vector3.one;
			canvasGroup.alpha = 1f;
			showHideSequence = DOTween.Sequence();
			showHideSequence.Append(rect.DOScale(Vector3.one * 1.1f, duration)).Join(canvasGroup.DOFade(0f, duration));
			await showHideSequence.WaitAsync();
		}
	}
}
