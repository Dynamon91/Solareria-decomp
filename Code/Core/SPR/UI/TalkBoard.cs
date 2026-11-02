using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LapinLibrary;
using SPR.L10N;
using TMPro;
using UnityEngine;

namespace SPR.UI;

public class TalkBoard : MonoBehaviour
{
	private enum ReadState
	{
		Appearing,
		Reading,
		EndReading,
		NextReady,
		UnTalking,
		Disappearing
	}

	[SerializeField]
	private TextMeshProUGUI nameText;

	[SerializeField]
	private SuperTextMesh speechText;

	[SerializeField]
	private MarkerIcon markerIcon;

	private CanvasGroup canvasGroup;

	private Animator animator;

	private bool isAutoReading;

	private ReadState readState;

	private LocaleText nameLocaleText;

	private LocaleText speechLocaleText;

	private CancellationTokenSource activeCancelToken;

	private static readonly int ActivateBool = Animator.StringToHash("Activate");

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	private LocaleText NameLocaleText => nameLocaleText ?? (nameLocaleText = nameText.GetComponent<LocaleText>());

	private LocaleText SpeechLocaleText => speechLocaleText ?? (speechLocaleText = speechText.GetComponent<LocaleText>());

	private bool IsAppearing => readState == ReadState.Appearing;

	public bool IsActive
	{
		get
		{
			ReadState readState = this.readState;
			return readState == ReadState.Reading || readState == ReadState.EndReading || readState == ReadState.NextReady || readState == ReadState.UnTalking;
		}
	}

	public bool IsReading => readState == ReadState.Reading;

	public bool NextReady => readState == ReadState.NextReady;

	public void Initialize()
	{
		CanvasGroup.alpha = 0f;
		nameText.text = "";
		speechText.text = "";
		markerIcon.SetObjectActive(isActive: false);
	}

	public async UniTask ActivateTalkAsync(string speakerName, string speakerText, float autoDuration, string voiceKey)
	{
		if (!IsActive)
		{
			markerIcon.SetObjectActive(isActive: false);
			Animator.SetBool(ActivateBool, value: true);
			readState = ReadState.Appearing;
			await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.99f, PlayerLoopTiming.Update, TaskUtil.RefreshToken(ref activeCancelToken));
			speechText.OnCompleteEvent += OnSpeechTextComplete;
		}
		else
		{
			await UniTask.WaitUntil(() => !markerIcon.IsTweenActive, PlayerLoopTiming.Update, TaskUtil.RefreshToken(ref activeCancelToken));
			markerIcon.SetObjectActive(isActive: false);
		}
		NameLocaleText.SetData(LocaleKey.ActorName(speakerName));
		SpeechLocaleText.SetRawText(speakerText);
		speechText.Read();
		SoundManager.PlayVoice(speakerName, voiceKey);
		readState = ReadState.Reading;
		isAutoReading = autoDuration > 0f;
	}

	public async UniTask DeactivateTalkAsync()
	{
		await UniTask.WaitUntil(() => !markerIcon.IsTweenActive);
		Animator.SetBool(ActivateBool, value: false);
		if (IsActive || IsAppearing)
		{
			readState = ReadState.Disappearing;
			activeCancelToken?.Cancel();
			await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.99f);
			speechText.OnCompleteEvent -= OnSpeechTextComplete;
			nameText.text = string.Empty;
			speechText.text = string.Empty;
		}
	}

	public void SkipToEnd()
	{
		speechText.SkipToEnd();
	}

	private void OnSpeechTextComplete()
	{
		if (!activeCancelToken.IsCancellationRequested)
		{
			if (isAutoReading)
			{
				readState = ReadState.NextReady;
				return;
			}
			readState = ReadState.EndReading;
			ActivateSpeechEndMarkerAsync().Forget();
		}
	}

	private async UniTask ActivateSpeechEndMarkerAsync()
	{
		await UniTask.WaitForSeconds(0.2f, ignoreTimeScale: false, PlayerLoopTiming.Update, TaskUtil.RefreshToken(ref activeCancelToken));
		markerIcon.SetObjectActive(isActive: true);
		readState = ReadState.NextReady;
	}

	public async UniTask NextScriptAsync(Action runNextScript)
	{
		readState = ReadState.UnTalking;
		if (!isAutoReading)
		{
			await markerIcon.ShakeVerticalAsync();
			markerIcon.SetObjectActive(isActive: false);
		}
		SoundManager.StopVoice();
		runNextScript?.Invoke();
	}
}
