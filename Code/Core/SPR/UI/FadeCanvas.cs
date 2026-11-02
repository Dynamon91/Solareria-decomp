using System.Threading;
using Coffee.UIExtensions;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Library;
using SPR.Library;
using SPR.UI.Fade;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class FadeCanvas : AutoScaledCanvas
{
	[Header("Reference")]
	[SerializeField]
	private Image fadeImage;

	[SerializeField]
	private SkipBoard skipBoard;

	[SerializeField]
	private LocaleText narrationText;

	[SerializeField]
	private UIParticle uiParticle;

	[SerializeField]
	private LoadingBoard loadingBoard;

	[SerializeField]
	private WarningBoard warningBoard;

	private Sequence narrateSequence;

	public SkipBoard SkipBoard => skipBoard;

	public LoadingBoard LoadingBoard => loadingBoard;

	public WarningBoard WarningBoard => warningBoard;

	public bool IsBlack => fadeImage.color.a >= 1f;

	public bool IsWhite => fadeImage.color.a <= 0f;

	public void Initialize()
	{
		SkipBoard.Initialize(0.5f);
		loadingBoard.Initialize();
		base.gameObject.SetActive(value: true);
		narrationText.SetColor(GraphicHelper.TransparentColor);
		narrationText.gameObject.SetActive(value: false);
		uiParticle.Stop();
		uiParticle.gameObject.SetActive(value: false);
		warningBoard.Initialize();
		narrateSequence?.Kill();
	}

	public async UniTask FadeIn(float duration, float predelay = 0f, Ease ease = Ease.Linear, CancellationToken token = default(CancellationToken), bool ignoreTimescale = false)
	{
		if (fadeImage.color.a == 0f)
		{
			fadeImage.gameObject.SetActive(value: false);
			return;
		}
		if (predelay > 0f)
		{
			await UniTaskExtension.WaitForSeconds(predelay, token);
		}
		TweenerCore<Color, Color, ColorOptions> tween = fadeImage.DOFade(0f, duration).SetEase(ease).SetUpdate(ignoreTimescale);
		await tween.WaitAsync(token);
		fadeImage.gameObject.SetActive(value: false);
		if (token.IsCancellationRequested)
		{
			tween.Kill();
		}
	}

	public async UniTask FadeOut(float duration, float postDelay = 0f, CancellationToken token = default(CancellationToken), bool ignoreTimescale = false)
	{
		if (fadeImage.color.a >= 0.99f)
		{
			fadeImage.gameObject.SetActive(value: true);
			return;
		}
		float a = fadeImage.color.a;
		fadeImage.color = new Color(0f, 0f, 0f, a);
		fadeImage.gameObject.SetActive(value: true);
		TweenerCore<Color, Color, ColorOptions> tween = fadeImage.DOFade(1f, duration).SetUpdate(ignoreTimescale);
		await tween.WaitAsync(token);
		if (token.IsCancellationRequested)
		{
			tween.Kill();
		}
		await UniTaskExtension.WaitForSeconds(postDelay, token);
	}

	public async UniTask FadeWhiteOut(float duration, float postDelay = 0f, CancellationToken token = default(CancellationToken))
	{
		if (fadeImage.color.a >= 0.99f)
		{
			fadeImage.gameObject.SetActive(value: true);
			return;
		}
		float a = fadeImage.color.a;
		fadeImage.color = new Color(1f, 1f, 1f, a);
		fadeImage.gameObject.SetActive(value: true);
		TweenerCore<Color, Color, ColorOptions> tween = fadeImage.DOFade(1f, duration);
		await tween.WaitAsync(token);
		if (token.IsCancellationRequested)
		{
			tween.Kill();
		}
		await UniTaskExtension.WaitForSeconds(postDelay, token);
	}

	public void FadeOutWhiteInstantly()
	{
		fadeImage.color = new Color(1f, 1f, 1f, 1f);
	}

	public async UniTask ShowNarrationAsync(CancellationToken token)
	{
		uiParticle.gameObject.SetActive(value: true);
		uiParticle.Play();
		narrationText.gameObject.SetActive(value: true);
		narrationText.SetColor(GraphicHelper.TransparentColor);
		narrateSequence = DOTween.Sequence();
		narrateSequence.AppendCallback(delegate
		{
			Game.Instance.SoundManager.PlayBGM("Born");
			Game.Instance.RuntimeData.SetBGMLessEvent(value: true);
		});
		for (int num = 1; num <= 3; num++)
		{
			int i1 = num;
			narrateSequence.AppendCallback(delegate
			{
				narrationText.SetData($"StartNarration_{i1}");
				Game.Instance.SoundManager.PlayNarrationEvent(i1 - 1);
			}).Append(narrationText.DOAlpha(1f, 1.5f)).AppendInterval(2f)
				.Append(narrationText.DOAlpha(0f, 1.5f));
		}
		await narrateSequence.WaitAsync(token);
		if (token.IsCancellationRequested)
		{
			narrateSequence.Kill();
			narrationText.gameObject.SetActive(value: false);
			uiParticle.Stop();
			uiParticle.gameObject.SetActive(value: false);
		}
		else
		{
			UniTaskExtension.WaitForSeconds(6f, token).GetAwaiter().OnCompleted(delegate
			{
				narrationText.gameObject.SetActive(value: false);
				uiParticle.Stop();
				uiParticle.gameObject.SetActive(value: false);
			});
		}
	}

	public async UniTask ShowNarrationAsync(string id, float fadeDuration, float duration, CancellationToken token = default(CancellationToken))
	{
		narrationText.gameObject.SetActive(value: true);
		narrationText.SetColor(GraphicHelper.TransparentColor);
		narrateSequence = DOTween.Sequence();
		narrateSequence.AppendCallback(delegate
		{
			narrationText.SetScenario(id);
		}).Append(narrationText.DOAlpha(1f, fadeDuration)).AppendInterval(duration)
			.Append(narrationText.DOAlpha(0f, fadeDuration));
		await narrateSequence.WaitAsync(token);
		if (token.IsCancellationRequested)
		{
			narrateSequence.Kill();
			narrationText.gameObject.SetActive(value: false);
			uiParticle.Stop();
			uiParticle.gameObject.SetActive(value: false);
		}
	}
}
