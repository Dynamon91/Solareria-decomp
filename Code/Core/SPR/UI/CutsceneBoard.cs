using System;
using System.Collections.Generic;
using Coffee.UIEffects;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using SPR.Library;
using Spine.Unity;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace SPR.UI;

public class CutsceneBoard : MonoBehaviour, IControllable
{
	[SerializeField]
	private RawImage memoryRenderRawImage;

	[SerializeField]
	private Image memoryBlackBackground;

	[SerializeField]
	private RenderTexture memoryCrystalRenderTexture;

	[SerializeField]
	private Camera renderCamera;

	[SerializeField]
	private Image memoryImage;

	[SerializeField]
	private LocaleText crystalText;

	[SerializeField]
	private CanvasGroup controlGuide;

	[SerializeField]
	private List<string> narrations;

	private SkeletonGraphic skeletonGraphic;

	private SpineCutscene currentCutscene;

	private Camera mainCamera;

	private PlayableDirector director;

	private Sequence nextMemorySequence;

	private Action onMemoryEnd;

	private int currentMemoryIndex;

	private List<(Sprite memorysprite, string narrationTextLocaleID)> memories;

	private string bgmSoundKey;

	private Camera MainCamera => mainCamera ?? (mainCamera = Camera.main);

	private PlayableDirector Director => director ?? (director = GetComponent<PlayableDirector>());

	private static InputManager InputManager => Game.Instance.InputManager;

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public bool IsValid => true;

	public void Initialize()
	{
		renderCamera.gameObject.SetActive(value: false);
		memoryCrystalRenderTexture.width = Screen.width;
		memoryCrystalRenderTexture.height = Screen.height;
		memoryRenderRawImage.color = GraphicHelper.TransparentColor;
		memoryRenderRawImage.texture = memoryCrystalRenderTexture;
		memoryBlackBackground.color = Color.black;
	}

	private void LateUpdate()
	{
		if (renderCamera.isActiveAndEnabled)
		{
			renderCamera.transform.position = MainCamera.transform.position;
			renderCamera.orthographicSize = MainCamera.orthographicSize;
		}
	}

	public void ActivateRenderTexture()
	{
		mainCamera = null;
		renderCamera.gameObject.SetActive(value: true);
		Director.Play();
		SoundManager.SetFadeAMBVolume(0f, 5f);
		SoundManager.FadeOutSFXEmitters(5f);
	}

	public void ActivateMemory(BossName bossName, Action memoryEndCallback = null)
	{
		Director.Play();
		onMemoryEnd = memoryEndCallback;
		memoryRenderRawImage.color = GraphicHelper.TransparentColor;
		memoryBlackBackground.color = Color.black;
		LoadMemoriesAsync(bossName).Forget();
		bgmSoundKey = "Memory_" + bossName;
	}

	public void SkipMemory()
	{
		Director.Pause();
		Director.time = Director.duration;
		Director.Evaluate();
		if (InputManager.ContainsInControlStack(this))
		{
			InputManager.RemoveControlTarget(this).Forget();
		}
		nextMemorySequence?.Kill();
		HideMemoryImage();
		renderCamera.gameObject.SetActive(value: false);
		memoryRenderRawImage.color = GraphicHelper.TransparentColor;
		memoryBlackBackground.GetComponent<UIDissolve>().effectFactor = 1f;
		onMemoryEnd?.Invoke();
		onMemoryEnd = null;
	}

	private async UniTask LoadMemoriesAsync(BossName bossName)
	{
		memories = new List<(Sprite, string)>();
		string prefix = $"Memory_{bossName}";
		int narrationAmount = Game.DataManager.SearchAllScenarioDataContains(prefix).Count;
		for (int i = 1; i <= narrationAmount; i++)
		{
			string text = $"{prefix}_{i}";
			string narrationTextLocaleID = text;
			Sprite item = null;
			if (AddressableHelper.HasKey(text))
			{
				AsyncOperationHandle<Sprite> spriteTask = Addressables.LoadAssetAsync<Sprite>(text);
				await spriteTask;
				item = spriteTask.Result;
			}
			memories.Add((item, narrationTextLocaleID));
		}
	}

	private void ShowMemory(int index)
	{
		var (memorySprite, localeID) = memories[index];
		nextMemorySequence?.Kill();
		nextMemorySequence = DOTween.Sequence();
		if (index == 0)
		{
			HideMemoryImage();
		}
		else
		{
			nextMemorySequence.Append(crystalText.TMP.DOFade(0f, 0.6f)).Join(controlGuide.DOFade(0f, 0.6f)).Append(memoryImage.DOFade(0f, 2f).SetEase(Ease.OutQuad));
		}
		nextMemorySequence.AppendCallback(delegate
		{
			if ((bool)memorySprite)
			{
				memoryImage.sprite = memorySprite;
			}
			crystalText.SetData(localeID);
		}).AppendInterval(0.5f).Append(memoryImage.DOFade(1f, 2f).SetEase(Ease.InQuart))
			.Append(crystalText.TMP.DOFade(1f, 0.6f))
			.Join(controlGuide.DOFade(1f, 0.6f));
	}

	public void PauseAndWaitInteractWithCrystal()
	{
		Director.Pause();
	}

	public void PauseAndShowMemoryImage()
	{
		Director.Pause();
		InputManager.StackControlTarget(this).Forget();
		SoundManager.FadeInBGM(bgmSoundKey, 5f);
		currentMemoryIndex = 0;
		ShowMemory(currentMemoryIndex);
	}

	public void HideMemoryImage()
	{
		memoryImage.color = GraphicHelper.TransparentColor;
		crystalText.TMP.color = GraphicHelper.TransparentColor;
		controlGuide.alpha = 0f;
	}

	public void EndTimeline()
	{
		onMemoryEnd?.Invoke();
		onMemoryEnd = null;
	}

	public string ControllerName()
	{
		return "Memory Crystal Board";
	}

	public UniTask Activate()
	{
		RectTransform rectTransform = memoryImage.rectTransform;
		float endValue = rectTransform.anchoredPosition.y - 30f;
		rectTransform.DOKill(complete: true);
		rectTransform.DOAnchorPosY(endValue, 2f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
		crystalText.gameObject.SetActive(value: true);
		controlGuide.gameObject.SetActive(value: true);
		controlGuide.DOFade(0f, 0f);
		return UniTask.CompletedTask;
	}

	public void Control()
	{
		if (InputManager.confirmDown)
		{
			if (nextMemorySequence.IsActive())
			{
				nextMemorySequence.Complete(withCallbacks: true);
				return;
			}
			if (memories.Count - 1 <= currentMemoryIndex)
			{
				InputManager.RemoveControlTarget(this).Forget();
				return;
			}
			currentMemoryIndex++;
			ShowMemory(currentMemoryIndex);
		}
	}

	public UniTask Pause()
	{
		return UniTask.CompletedTask;
	}

	public UniTask Restart()
	{
		return UniTask.CompletedTask;
	}

	public UniTask Deactivate()
	{
		Director.Play();
		SoundManager.SetFadeAMBVolume(1f, 4f);
		SoundManager.FadeOutBGM(4f);
		SoundManager.FadeInSFXEmitters(4f);
		crystalText.gameObject.SetActive(value: false);
		controlGuide.gameObject.SetActive(value: false);
		renderCamera.gameObject.SetActive(value: false);
		bgmSoundKey = null;
		return UniTask.CompletedTask;
	}
}
