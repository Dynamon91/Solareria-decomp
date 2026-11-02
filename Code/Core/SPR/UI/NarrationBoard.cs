using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SPR.UI;

public class NarrationBoard : MonoBehaviour
{
	[Header("Duration")]
	[SerializeField]
	private float totalSeconds;

	[SerializeField]
	private float fadeSeconds;

	[Header("References")]
	[SerializeField]
	private SuperTextMesh narrationText;

	[SerializeField]
	private TextMeshProUGUI narrationTMP;

	[SerializeField]
	private GameObject controlGuide;

	private LocaleText narrationLocaleText;

	private CanvasGroup canvasGroup;

	private Sequence sequence;

	private string voiceActor;

	private string voiceKey;

	public float TotalSeconds => totalSeconds;

	public bool ControlGuideActive => controlGuide.activeSelf;

	public bool IsActive => CanvasGroup.alpha > 0f;

	private LocaleText NarrationLocaleText => narrationLocaleText ?? (narrationLocaleText = narrationText?.GetComponent<LocaleText>() ?? narrationTMP.GetComponent<LocaleText>());

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public void Initialize()
	{
		base.gameObject.SetActive(value: true);
		CanvasGroup.alpha = 0f;
		voiceActor = null;
		voiceKey = null;
		base.gameObject.SetActive(value: false);
	}

	public void SetLocale(string key)
	{
		NarrationLocaleText.SetData(key);
	}

	public void SetScenario(string id)
	{
		NarrationLocaleText.SetScenario(id);
	}

	public void SetScenario(ScenarioData data)
	{
		NarrationLocaleText.SetScenario(data);
	}

	public void Clear()
	{
		NarrationLocaleText.ClearText();
	}

	public void SetVoice(string actor, string voiceKey)
	{
		voiceActor = actor;
		this.voiceKey = voiceKey;
	}

	public void SetActiveDuration()
	{
		SetActiveDuration(totalSeconds, fadeSeconds);
	}

	public void SetActiveDuration(float totalDuration, float fadeDuration)
	{
		CanvasGroup.alpha = 0f;
		base.gameObject.SetActive(value: true);
		if (totalDuration < 2f * fadeDuration)
		{
			fadeDuration = totalDuration / 2f;
		}
		SoundManager.PlayVoice(voiceActor, voiceKey);
		voiceActor = null;
		voiceKey = null;
		sequence?.Kill();
		sequence = DOTween.Sequence();
		sequence.Append(CanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutCubic)).AppendInterval(Mathf.Max(0f, totalDuration - 2f * fadeDuration)).Append(CanvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InCubic));
	}

	public void SetActiveContinue(float fadeDuration)
	{
		CanvasGroup.alpha = 0f;
		base.gameObject.SetActive(value: true);
		SoundManager.PlayVoice(voiceActor, voiceKey);
		voiceActor = null;
		voiceKey = null;
		sequence?.Kill();
		sequence = DOTween.Sequence();
		sequence.Append(CanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutCubic));
	}

	public void DisableNow(bool disableControlGuide = false)
	{
		sequence?.Kill();
		CanvasGroup.alpha = 0f;
		if (disableControlGuide)
		{
			controlGuide.SetActive(value: false);
		}
	}

	public void FadeOutNow(float fadeDuration, bool disableControlGuide = false)
	{
		sequence?.Kill();
		sequence = DOTween.Sequence();
		sequence.Append(CanvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InCubic));
		if (disableControlGuide)
		{
			controlGuide.SetActive(value: false);
		}
	}

	public void EnableControlGuide(bool enable)
	{
		controlGuide.SetActive(enable);
	}
}
